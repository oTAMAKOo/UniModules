﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Console;
using Modules.UniRxExtension;

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

        // Coroutine中断用.
        private YieldCancel yieldCancel = null;

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

            // 中断用登録.
            yieldCancel = new YieldCancel();

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
            InstallDirectory = PathUtility.Combine(directory, "ExternalResources");

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

        /// <summary>
        /// 暗号化キー設定.
        /// Key,IVがModules.ExternalResource.Editor.ManageConfigのAssetのCryptKeyと一致している必要があります.
        /// </summary>
        /// <param name="key">暗号化Key(32文字)</param>
        /// <param name="iv">暗号化IV(16文字)</param>
        public void SetCryptoKey(string key, string iv)
        {
            assetBundleManager.SetCryptoKey(key, iv);
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
            assetInfosByResourcePath = allAssetInfos.ToDictionary(x => x.ResourcePath);

            // アセットバンドル依存関係.
            var dependencies = allAssetInfos
                .Where(x => x.IsAssetBundle)
                .Select(x => x.AssetBundle)
                .Where(x => x.Dependencies != null && x.Dependencies.Any())
                .GroupBy(x => x.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToDictionary(x => x.AssetBundleName, x => x.Dependencies);

            assetBundleManager.SetDependencies(dependencies);
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

        /// <summary> キャッシュ削除. </summary>
        public void CleanCache()
        {
            UnloadAllAssetBundles(false);

            ClearVersion();

            DirectoryUtility.Clean(InstallDirectory);

            Caching.ClearCache();

            GC.Collect();
        }

        /// <summary>
        /// マニフェストファイルを更新.
        /// </summary>
        public IObservable<Unit> UpdateManifest()
        {
            return Observable.FromCoroutine(() => UpdateManifestInternal());
        }

        private IEnumerator UpdateManifestInternal()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // アセット管理情報読み込み.

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

            var assetPath = PathUtility.Combine(resourceDirectory, manifestAssetInfo.ResourcePath);

            // AssetInfoManifestは常に最新に保たなくてはいけない為必ずダウンロードする.
            var loadYield = assetBundleManager.UpdateAssetInfoManifest()
                .SelectMany(_ => assetBundleManager.LoadAsset<AssetInfoManifest>(manifestAssetInfo, assetPath))
                .ToYieldInstruction(false);

            yield return loadYield;

            if (loadYield.HasError || loadYield.IsCanceled)
            {
                yield break;
            }

            SetAssetInfoManifest(loadYield.Result);

            sw.Stop();

            if (LogEnable && UnityConsole.Enable)
            {
                var message = string.Format("UpdateManifest: ({0:F2}ms)", sw.Elapsed.TotalMilliseconds);

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }

            // アセット管理情報を登録.

            assetBundleManager.SetManifest(assetInfoManifest);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetManifest(assetInfoManifest);

            #endif
        }

        /// <summary>
        /// アセットを更新.
        /// </summary>
        public static IObservable<Unit> UpdateAsset(string resourcePath, IProgress<float> progress = null)
        {
            return Observable.FromCoroutine(() => instance.UpdateAssetInternal(resourcePath, progress));
        }

        private IEnumerator UpdateAssetInternal(string resourcePath, IProgress<float> progress = null)
        {
            if (string.IsNullOrEmpty(resourcePath)) { yield break; }

            var assetInfo = GetAssetInfo(resourcePath);

            if (assetInfo == null)
            {
                var exception = new Exception(string.Format("AssetManageInfo not found.\n{0}", resourcePath));

                OnError(exception);

                yield break;
            }

            // 外部処理.

            if (instance.updateAssetHandler != null)
            {
                var updateRequestYield = instance.updateAssetHandler
                    .OnUpdateRequest(assetInfo)
                    .ToYieldInstruction(false, yieldCancel.Token);

                while (!updateRequestYield.IsDone)
                {
                    yield return null;
                }

                if (updateRequestYield.HasError)
                {
                    OnError(updateRequestYield.Error);

                    yield break;
                }
            }

            // ローカルモードなら更新しない.

            if (!instance.LocalMode)
            {
                #if ENABLE_CRIWARE_FILESYSTEM

                var extension = Path.GetExtension(resourcePath);
                
                if (IsCriAsset(extension))
                {
                    var updateCriAssetYield = UpdateCriAsset(assetInfo, progress).ToYieldInstruction();

                    while (!updateCriAssetYield.IsDone)
                    {
                        yield return null;
                    }

                    if (!updateCriAssetYield.HasResult || !updateCriAssetYield.Result)
                    {
                        yield break;
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

                        yield break;
                    }

                    var updateYield = instance.assetBundleManager
                        .UpdateAssetBundle(assetInfo, progress)
                        .ToYieldInstruction(false, yieldCancel.Token);

                    while (!updateYield.IsDone)
                    {
                        yield return null;
                    }

                    if (updateYield.IsCanceled || updateYield.HasError)
                    {
                        yield break;
                    }
                }

                // バージョン更新.
                UpdateVersion(resourcePath);
            }

            // 外部処理.

            if (instance.updateAssetHandler != null)
            {
                var updateFinishYield = instance.updateAssetHandler
                    .OnUpdateFinish(assetInfo)
                    .ToYieldInstruction(false, yieldCancel.Token);

                while (!updateFinishYield.IsDone)
                {
                    yield return null;
                }

                if (updateFinishYield.HasError)
                {
                    OnError(updateFinishYield.Error);

                    yield break;
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
            if (yieldCancel != null)
            {
                yieldCancel.Dispose();

                // キャンセルしたので再生成.
                yieldCancel = new YieldCancel();

                assetBundleManager.RegisterYieldCancel(yieldCancel);
            }
        }

        public static string GetAssetPathFromAssetInfo(string externalResourcesPath, string shareResourcesPath, AssetInfo assetInfo)
        {
            var assetPath = string.Empty;

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
