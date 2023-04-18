
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC
﻿﻿﻿
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using CriWare;
using UniRx;
using Extensions;
using Modules.ExternalAssets;

namespace Modules.CriWare
{
    public sealed partial class CriAssetManager : Singleton<CriAssetManager>
    {
        //----- params -----

		// タイムアウトまでの時間.
        private readonly TimeSpan TimeoutLimit = TimeSpan.FromSeconds(180f);

        // リトライする回数.
        private readonly int RetryCount = 3;

        // リトライするまでの時間(秒).
        private readonly TimeSpan RetryDelaySeconds = TimeSpan.FromSeconds(2f);

        //----- field -----

        // アセット管理.
        private AssetInfoManifest manifest = null;

		// ダウンロード元URL.
        private string remoteUrl = null;
        private string versionHash = null;

        // シュミュレートモードか.
        private bool simulateMode = false;

        // ローカルモードか.
        private bool localMode = false;

		#if ENABLE_CRIWARE_FILESYSTEM

        // インストーラー.
        private List<CriFsWebInstaller> installers = null;

        // ダウンロード待ち.
        private Dictionary<string, CriAssetInstall> installQueueing = null;

        #endif

        // 同時インストール数.
        private uint numInstallers = 5;

        // イベント通知.
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private bool isInitialized = false;

        //----- property -----

        //----- method -----

        private CriAssetManager() { }

        public void Initialize(bool simulateMode = false)
        {
            if (isInitialized) { return; }
			
            this.simulateMode = UnityUtility.isEditor && simulateMode;

            #if ENABLE_CRIWARE_FILESYSTEM

            installQueueing = new Dictionary<string, CriAssetInstall>();
            installers = new List<CriFsWebInstaller>();

            //------ CriInstaller初期化 ------

            UpdateFsWebInstallerSetting();

            Observable.EveryUpdate()
                .Subscribe(_ => CriFsWebInstaller.ExecuteMain())
                .AddTo(Disposable);

            #endif

            isInitialized = true;
        }

        protected override void OnRelease()
        {
            if (!isInitialized){ return; }

            ReleaseInstaller();
        }

        private void ReleaseInstaller()
        {
            #if ENABLE_CRIWARE_FILESYSTEM

            if (installers != null)
            {
                foreach (var installer in installers)
                {
                    installer.Stop();
                    installer.Dispose();
                }

                installers.Clear();
            }

            if (installQueueing != null)
            {
                foreach (var item in installQueueing.Values)
                {
                    if (item.Installer != null)
                    {
                        item.Installer.Stop();
                        item.Installer.Dispose();
                    }
                }

                installQueueing.Clear();
            }

            if(CriFsWebInstaller.isInitialized)
            {
                CriFsWebInstaller.FinalizeModule();
            }

            #endif
        }

        private void UpdateFsWebInstallerSetting()
        {
            if(CriFsWebInstaller.isInitialized)
            {
                ReleaseInstaller();
            }

            var moduleConfig = CriFsWebInstaller.defaultModuleConfig;

            // 同時インストール数.
            moduleConfig.numInstallers = numInstallers;
            // タイムアウト時間.
            moduleConfig.inactiveTimeoutSec = (uint)TimeoutLimit.TotalSeconds;
            
            CriFsWebInstaller.InitializeModule(moduleConfig);
        }

        /// <summary> 同時ダウンロード数設定. </summary>
        public void SetNumInstallers(uint numInstallers)
        {
            this.numInstallers = numInstallers;

            UpdateFsWebInstallerSetting();
        }

        /// <summary> ローカルモード設定. </summary>
        public void SetLocalMode(bool localMode)
        {
            this.localMode = localMode;
        }

		/// <summary> URLを設定. </summary>
        public void SetUrl(string remoteUrl, string versionHash)
        {
            this.remoteUrl = remoteUrl;
            this.versionHash = versionHash;
        }

        public void SetManifest(AssetInfoManifest manifest)
        {
            this.manifest = manifest;
        }

        #if ENABLE_CRIWARE_FILESYSTEM

        /// <summary> CRIアセットを更新. </summary>
        public async UniTask UpdateCriAsset(string installPath, AssetInfo assetInfo, IProgress<float> progress = null, CancellationToken cancelToken = default)
        {
            if (simulateMode || localMode) { return; }
			
			var install = GetCriAssetInstall(installPath, assetInfo, progress);

			await install.Task
				.Timeout(TimeoutLimit)
                .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds)
                .DoOnError(ex => OnError(ex))
                .Finally(() => RemoveInternalQueue(install))
                .ToUniTask(cancellationToken: cancelToken);
        }

        private CriAssetInstall GetCriAssetInstall(string installPath, AssetInfo assetInfo, IProgress<float> progress)
        {
            var install = installQueueing.GetValueOrDefault(assetInfo.ResourcePath);

            if (install != null) { return install; }

            install = new CriAssetInstall(installPath, assetInfo, progress);

            installQueueing[assetInfo.ResourcePath] = install;

            return install;
        }

        private void RemoveInternalQueue(CriAssetInstall install)
        {
            if (install == null) { return; }

            if (install.AssetInfo == null) { return; }

            var resourcePath = install.AssetInfo.ResourcePath;

            var item = installQueueing.GetValueOrDefault(resourcePath);

            if (item != null)
            {
                if (item.Installer != null)
                {
                    item.Installer.Stop();
                }

                installQueueing.Remove(resourcePath);
            }

            // インストーラ解放.
            if (installQueueing.IsEmpty())
            {
                foreach (var installer in installers)
                {
                    installer.Stop();
                    installer.Dispose();
                }

                installers.Clear();
            }
        }

        #endif

		public string BuildDownloadUrl(AssetInfo assetInfo)
        {
            var platformName = PlatformUtility.GetPlatformTypeName();

            var url = PathUtility.Combine(new string[] { remoteUrl, platformName, versionHash, assetInfo.FileName });

            return $"{url}?v={assetInfo.Hash}";
        }

        public bool IsCriAsset(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            return CriAssetDefinition.AssetAllExtensions.Any(y => y == extension);
        }

		public bool IsCriInstallTempFile(string filePath)
		{
			return CriAssetDefinition.InstallTempFileAllExtensions.Any(x => filePath.EndsWith(x));
		}

		public string GetFilePath(string installPath, AssetInfo assetInfo)
        {
			if (assetInfo == null){ return null; }

			var streamingAssetsPath = UnityPathUtility.StreamingAssetsPath;

			installPath = PathUtility.ConvertPathSeparator(installPath);

			// CRIはフルパスでない場合、StreamingAssetsからのパスを参照する.
			if (installPath.StartsWith(streamingAssetsPath))
			{
				installPath = installPath.Replace(streamingAssetsPath, string.Empty).TrimStart(PathUtility.PathSeparator);
			}

			return PathUtility.Combine(installPath, assetInfo.FileName);
        }

        private void OnTimeout(AssetInfo assetInfo, Exception exception)
        {
			if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
            }
			else
			{
				Debug.LogErrorFormat("[Download Timeout] \n{0}", exception);
			}
        }

        private void OnError(Exception exception)
        {
			if (onError != null)
            {
                onError.OnNext(exception);
            }
			else
			{
				Debug.LogErrorFormat("[Download Error] \n{0}", exception);
			}
        }

        /// <summary>
        /// タイムアウト時のイベント.
        /// </summary>
        /// <returns></returns>
        public IObservable<AssetInfo> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<AssetInfo>());
        }

        /// <summary>
        /// エラー時のイベント.
        /// </summary>
        public IObservable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }
    }
}

#endif
