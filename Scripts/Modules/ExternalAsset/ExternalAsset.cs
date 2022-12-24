﻿
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset : Singleton<ExternalAsset>
    {
        //----- params -----

        public static readonly string ConsoleEventName = "ExternalAsset";
        public static readonly Color ConsoleEventColor = new Color(0.8f, 1f, 0.1f);

		//----- field -----

        // アセット管理情報.
        private AssetInfoManifest assetInfoManifest = null;

		// アセットGUIDパスをキーとしたアセット情報.
		private Dictionary<string, AssetInfo> assetInfosByAssetGuid = null;

        // アセットロードパスをキーとしたアセット情報.
        private Dictionary<string, AssetInfo> assetInfosByResourcePath = null;

        /// <summary> シュミレーションモード (Editorのみ有効). </summary>
        private bool simulateMode = false;

        /// <summary> 外部アセットディレクトリ. </summary>
        public string externalAssetDirectory = null;

        /// <summary> 読み込み中アセット群. </summary>
        private HashSet<AssetInfo> loadingAssets = new HashSet<AssetInfo>();

        // 中断用.
        private CancellationTokenSource cancelSource = null;

        // 外部制御ハンドラ.

        private IUpdateAssetHandler updateAssetHandler = null;
        private ILoadAssetHandler loadAssetHandler = null;

        // イベント通知.
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private Subject<string> onUpdateAsset = null;
        private Subject<string> onLoadAsset = null;
        private Subject<string> onUnloadAsset = null;

        private bool initialized = false;

        //----- property -----

        public static bool Initialized
        {
            get { return Instance != null && Instance.initialized; }
        }

        /// <summary> ローカルモード. </summary>
        public bool LocalMode { get; private set; }

        /// <summary> ダウンロード先. </summary>
        public string InstallDirectory { get; private set; }

        /// <summary> ログ出力が有効. </summary>
        public bool LogEnable { get; set; }

		/// <summary> 最大同時ダウンロード数.  </summary>
		public uint MaxDownloadCount { get; set; } = 10;

        //----- method -----

        private ExternalAsset()
        {
            LogEnable = true;
        }

        public void Initialize(string externalAssetDirectory, string shareAssetDirectory)
        {
            if (initialized) { return; }

            this.externalAssetDirectory = externalAssetDirectory;
            this.shareAssetDirectory = shareAssetDirectory;

            // 中断用.
			cancelSource = new CancellationTokenSource();

            //----- AssetBundleManager初期化 -----
                        
            #if UNITY_EDITOR

            simulateMode = Prefs.isSimulate;

            #endif

            // AssetBundleManager初期化.

            InitializeAssetBundle();

            // CRI初期化.

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            InitializeCri();
            
            #endif

            // 保存先設定.
            SetInstallDirectory(Application.persistentDataPath);

            initialized = true;
        }

        /// <summary> ローカルモード設定. </summary>
        public void SetLocalMode(bool localMode)
        {
            LocalMode = localMode;

            assetBundleManager.SetLocalMode(localMode);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetLocalMode(localMode);

            #endif
        }

        /// <summary> 保存先ディレクトリ設定. </summary>
        public void SetInstallDirectory(string directory)
        {
            InstallDirectory = PathUtility.Combine(directory, "Contents");

            #if UNITY_IOS

            if (InstallDirectory.StartsWith(Application.persistentDataPath))
            {
                UnityEngine.iOS.Device.SetNoBackupFlag(InstallDirectory);
            }

            #endif
		}

        /// <summary> URLを設定. </summary>
        public void SetUrl(string remoteUrl, string versionHash)
        {
            assetBundleManager.SetUrl(remoteUrl, versionHash);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetUrl(remoteUrl, versionHash);

            #endif
        }

		// アセット管理マニュフェスト情報を更新.
		private async UniTask SetAssetInfoManifest(AssetInfoManifest manifest)
		{
			assetInfoManifest = manifest;

			if(manifest == null)
			{
				Debug.LogError("AssetInfoManifest not found.");
				return;
			}

			assetInfosByAssetBundleName = new Dictionary<string, List<AssetInfo>>();
			assetInfosByAssetGuid = new Dictionary<string, AssetInfo>();
			assetInfosByResourcePath = new Dictionary<string, AssetInfo>();

			var assetInfos = manifest.GetAssetInfos();

			var count = 0;

			foreach (var assetInfo in assetInfos)
			{
				// アセット情報 (Key: アセットバンドル名).
				if (assetInfo.IsAssetBundle)
				{
					var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

					var list = assetInfosByAssetBundleName.GetValueOrDefault(assetBundleName);

					if (list == null)
					{
						list = new List<AssetInfo>();
						assetInfosByAssetBundleName[assetBundleName] = list;
					}

					list.Add(assetInfo);
				}

				// アセット情報 (Key: アセットGUID).
				if (!string.IsNullOrEmpty(assetInfo.Guid))
				{
					assetInfosByAssetGuid[assetInfo.Guid] = assetInfo;
				}
                
				// アセット情報 (Key: リソースパス).
				if (!string.IsNullOrEmpty(assetInfo.ResourcePath))
				{
					assetInfosByResourcePath[assetInfo.ResourcePath] = assetInfo;
				}

				if(++count % 1000 == 0)
				{
					await UniTask.NextFrame();
				}
			}
		}

         /// <summary>
        /// アセット管理情報を取得.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public IEnumerable<AssetInfo> GetGroupAssetInfos(string groupName = null)
        {
            return assetInfoManifest.GetAssetInfos(groupName);
        }

		/// <summary> アセット情報取得 </summary>
        public AssetInfo GetAssetInfo(string resourcePath)
        {
            return assetInfosByResourcePath.GetValueOrDefault(resourcePath);
        }

		/// <summary> アセット情報取得 </summary>
		public AssetInfo GetAssetInfoByGuid(string guid)
		{
			return assetInfosByAssetGuid.GetValueOrDefault(guid);
		}

		/// <summary> マニフェストファイルを更新. </summary>
		public async UniTask<bool> UpdateManifest()
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			// アセット管理情報読み込み.

			var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

			var assetPath = PathUtility.Combine(externalAssetDirectory, manifestAssetInfo.ResourcePath);

			// AssetInfoManifestは常に最新に保たなくてはいけない為必ずダウンロードする.

			await assetBundleManager.UpdateAssetInfoManifest(InstallDirectory, cancelSource.Token);

			var manifest = await assetBundleManager.LoadAsset<AssetInfoManifest>(InstallDirectory, manifestAssetInfo, assetPath);

			if (manifest == null)
			{
				throw new FileNotFoundException("Failed update AssetInfoManifest.");
			}

			await SetAssetInfoManifest(manifest);

			// アセット管理情報を登録.

			await assetBundleManager.SetManifest(assetInfoManifest);

			#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

			criAssetManager.SetManifest(assetInfoManifest);

			#endif

			sw.Stop();

			if (LogEnable && UnityConsole.Enable)
			{
				var message = $"UpdateManifest: ({sw.Elapsed.TotalMilliseconds:F2}ms)";

				UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
			}

			// 不要になったファイル削除.
			try
			{
				await DeleteUnUsedCache();
			}
			catch (Exception e)
			{
				Debug.LogException(e);

				return false;
			}

			return true;
		}

        /// <summary>
        /// アセットを更新.
        /// </summary>
        public static async UniTask UpdateAsset(string resourcePath, IProgress<float> progress = null)
        {
            await instance.UpdateAssetInternal(resourcePath, progress);
        }

        private async UniTask UpdateAssetInternal(string resourcePath, IProgress<float> progress = null)
        {
            if (string.IsNullOrEmpty(resourcePath)) { return; }

            var assetInfo = GetAssetInfo(resourcePath);

            if (assetInfo == null)
            {
                var exception = new Exception(string.Format("AssetManageInfo not found.\n{0}", resourcePath));

                OnError(exception);

                return;
            }

            // 外部処理.

            if (instance.updateAssetHandler != null)
            {
				try
				{
					var updateAssetHandler = instance.updateAssetHandler;

					await updateAssetHandler.OnUpdateRequest(assetInfo).AttachExternalCancellation(cancelSource.Token);
				}
				catch (Exception e)
				{
					OnError(e);
					return;
				}
			}
			
			if (!LocalMode && !simulateMode)
			{
                #if ENABLE_CRIWARE_FILESYSTEM

                var extension = Path.GetExtension(resourcePath);
                
                if (IsCriAsset(extension))
                {
					try
					{
						await UpdateCriAsset(cancelSource.Token, assetInfo, progress);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
						return;
					}
				}
                else
                
                #endif

                {
                    // ローカルバージョンが最新の場合は更新しない.
                    if (CheckAssetBundleVersion(assetInfo))
                    {
                        if(progress != null)
                        {
                            progress.Report(1f);
                        }

                        return;
                    }

					try
					{
						var assetBundleManager = instance.assetBundleManager;

						await assetBundleManager.UpdateAssetBundle(InstallDirectory, assetInfo, progress).AttachExternalCancellation(cancelSource.Token);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
						return;
					}
				}

                // バージョン更新.
                UpdateVersion(resourcePath);
            }

            // 外部処理.

            if (instance.updateAssetHandler != null)
            {
				try
				{
					var updateAssetHandler = instance.updateAssetHandler;

					await updateAssetHandler.OnUpdateFinish(assetInfo).AttachExternalCancellation(cancelSource.Token);
				}
				catch (Exception e)
				{
					OnError(e);

					return;
				}
			}

            // イベント発行.

            if (onUpdateAsset != null)
            {
                onUpdateAsset.OnNext(resourcePath);
            }
        }

        public void CancelAll()
        {
            if (cancelSource != null)
            {
				cancelSource.Cancel();

                // キャンセルしたので再生成.
				cancelSource = new CancellationTokenSource();
            }
        }

        public static string GetAssetPathFromAssetInfo(string externalAssetPath, string shareResourcesPath, AssetInfo assetInfo)
        {
            var assetPath = string.Empty;

            if (string.IsNullOrEmpty(assetInfo.ResourcePath)){ return null; }

            if (HasSharePrefix(assetInfo.ResourcePath))
            {
                var path = ConvertToShareResourcePath(assetInfo.ResourcePath);

                assetPath = PathUtility.Combine(shareResourcesPath, path);
            }
            else
            {
                assetPath = PathUtility.Combine(externalAssetPath, assetInfo.ResourcePath);
            }

            return assetPath;
        }

        #region Extend Handler

        public void SetUpdateAssetHandler(IUpdateAssetHandler handler)
        {
            this.updateAssetHandler = handler;
        }

        public void SetLoadAssetHandler(ILoadAssetHandler handler)
        {
            this.loadAssetHandler = handler;
        }

        #endregion

        private void OnTimeout(AssetInfo assetInfo)
        {
            Debug.LogErrorFormat("Timeout {0}", assetInfo.ResourcePath);

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
            }
        }

        private void OnError(Exception exception)
        {
            Debug.LogException(exception);

            if (onError != null)
            {
                onError.OnNext(exception);
            }
        }

        /// <summary> タイムアウト時イベント. </summary>
        public IObservable<AssetInfo> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<AssetInfo>());
        }

        /// <summary> エラー時イベント. </summary>
        public IObservable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }

        /// <summary> アセット更新時イベント. </summary>
        public IObservable<string> OnUpdateAssetAsObservable()
        {
            return onUpdateAsset ?? (onUpdateAsset = new Subject<string>());
        }

        /// <summary> アセット読み込み時イベント. </summary>
        public IObservable<string> OnLoadAssetAsObservable()
        {
            return onLoadAsset ?? (onLoadAsset = new Subject<string>());
        }

        /// <summary> アセット解放時イベント. </summary>
        public IObservable<string> OnUnloadAssetAsObservable()
        {
            return onUnloadAsset ?? (onUnloadAsset = new Subject<string>());
        }
    }
}
