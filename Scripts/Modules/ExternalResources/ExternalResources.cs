﻿﻿﻿﻿﻿
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

namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources : Singleton<ExternalResources>
    {
        //----- params -----

        public static readonly string ConsoleEventName = "ExternalResources";
        public static readonly Color ConsoleEventColor = new Color(0.8f, 1f, 0.1f);

        // 最大同時ダウンロード数.
        private readonly uint MaxDownloadCount = 4;

        //----- field -----

        // アセット管理情報.
        private AssetInfoManifest assetInfoManifest = null;

        // アセットロードパスをキーとしたアセット情報.
        private Dictionary<string, AssetInfo> assetInfosByResourcePath = null;

        /// <summary> シュミレーションモード (Editorのみ有効). </summary>
        private bool simulateMode = false;

        /// <summary> 外部アセットディレクトリ. </summary>
        public string resourceDirectory = null;

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

        //----- method -----

        private ExternalResources()
        {
            LogEnable = true;
        }

        public void Initialize(string resourceDirectory, string shareDirectory)
        {
            if (initialized) { return; }

            this.resourceDirectory = resourceDirectory;
            this.shareDirectory = shareDirectory;

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

            assetBundleManager.SetInstallDirectory(InstallDirectory);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetInstallDirectory(InstallDirectory);

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
        private void SetAssetInfoManifest(AssetInfoManifest manifest)
        {
            assetInfoManifest = manifest;

            if(manifest == null)
            {
                Debug.LogError("AssetInfoManifest not found.");
                return;
            }

            var allAssetInfos = manifest.GetAssetInfos().ToArray();

            // アセット情報 (Key: アセットバンドル名).
            assetInfosByAssetBundleName = allAssetInfos
                .Where(x => x.IsAssetBundle)
                .ToLookup(x => x.AssetBundle.AssetBundleName);

            // アセット情報 (Key: リソースパス).
            assetInfosByResourcePath = allAssetInfos
                .Where(x => !string.IsNullOrEmpty(x.ResourcePath))
                .ToDictionary(x => x.ResourcePath);

            // アセットバンドル依存関係.
            var dependencies = allAssetInfos
                .Where(x => x.IsAssetBundle)
                .Select(x => x.AssetBundle)
                .Where(x => x.Dependencies != null && x.Dependencies.Any())
                .GroupBy(x => x.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToDictionary(x => x.AssetBundleName, x => x.Dependencies);
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

		/// <summary> マニフェストファイルを更新. </summary>
		public async UniTask<bool> UpdateManifest()
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			// アセット管理情報読み込み.

			var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

			var assetPath = PathUtility.Combine(resourceDirectory, manifestAssetInfo.ResourcePath);

			// AssetInfoManifestは常に最新に保たなくてはいけない為必ずダウンロードする.

			await assetBundleManager.UpdateAssetInfoManifest(cancelSource.Token);

			var manifest = await assetBundleManager.LoadAsset<AssetInfoManifest>(manifestAssetInfo, assetPath);

			if (manifest == null)
			{
				throw new FileNotFoundException("Failed update AssetInfoManifest.");
			}

			SetAssetInfoManifest(manifest);

			sw.Stop();

			if (LogEnable && UnityConsole.Enable)
			{
				var message = $"UpdateManifest: ({sw.Elapsed.TotalMilliseconds:F2}ms)";

				UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
			}

			// アセット管理情報を登録.

			assetBundleManager.SetManifest(assetInfoManifest);

			#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

			criAssetManager.SetManifest(assetInfoManifest);

			#endif

			// 不要になったファイル削除.
			try
			{
				await DeleteDisUsedCache();
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

            // ローカルモードなら更新しない.

            if (!instance.LocalMode)
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

						await assetBundleManager.UpdateAssetBundle(assetInfo, progress).AttachExternalCancellation(cancelSource.Token);
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

        public static string GetAssetPathFromAssetInfo(string externalResourcesPath, string shareResourcesPath, AssetInfo assetInfo)
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
                assetPath = PathUtility.Combine(externalResourcesPath, assetInfo.ResourcePath);
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
