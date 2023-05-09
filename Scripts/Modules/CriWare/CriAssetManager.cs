
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

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

		// デフォルトインストーラ数.
		private const int DefaultInstallerNum = 8;

		// タイムアウトまでの時間.
        private readonly TimeSpan TimeoutLimit = TimeSpan.FromSeconds(180f);

		// インストーラ解放までの再利用待機時間.
        private readonly TimeSpan UnUseInstallerReleaseDelay = TimeSpan.FromSeconds(30f);

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
        private uint numInstallers = DefaultInstallerNum;

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

			Release();
        }

        private void Release()
        {
            #if ENABLE_CRIWARE_FILESYSTEM

			ClearInstallQueue();

            if (installers != null)
            {
                foreach (var installer in installers)
                {
                    installer.Stop();
                    installer.Dispose();
                }

                installers.Clear();
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
				Release();
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
			
			var install = GetCriAssetInstall(installPath, assetInfo, progress, cancelToken);

			await install.Task
                .DoOnError(ex => OnError(ex))
                .Finally(() => RemoveInternalQueue(install))
                .ToUniTask(cancellationToken: cancelToken);
        }

        private CriAssetInstall GetCriAssetInstall(string installPath, AssetInfo assetInfo, IProgress<float> progress, CancellationToken cancelToken)
        {
            var install = installQueueing.GetValueOrDefault(assetInfo.ResourcePath);

            if (install != null) { return install; }

            install = new CriAssetInstall(installPath, assetInfo, progress, cancelToken);

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
                var releaseInstallers = installers.ToArray();

				foreach (var releaseInstaller in releaseInstallers)
				{
					ReleaseUnUseInstaller(releaseInstaller).Forget();
				}
			}
        }

		private async UniTask ReleaseUnUseInstaller(CriFsWebInstaller installer)
        {
            if (installer == null){ return; }

            installer.Stop();

            // 解放時間.
            var releaseTime = DateTime.UtcNow + UnUseInstallerReleaseDelay;

			// 解放まで一定時間待つ.
			while (DateTime.UtcNow < releaseTime)
			{
				var statusInfo = installer.GetStatusInfo();

				// 再利用されていたら解放キャンセル.
				if (statusInfo.status == CriFsWebInstaller.Status.Busy){ return; }
				
                await UniTask.NextFrame();
			}

			// 解放.
			if (installers.Contains(installer))
			{
				installers.Remove(installer);

				installer.Dispose();
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

		public void ClearInstallQueue()
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

        private void OnTimeout(AssetInfo assetInfo, Exception exception)
        {
			if (onTimeOut != null)
            {
				Debug.LogErrorFormat("[Download Timeout] \n{0}", exception);

				onTimeOut.OnNext(assetInfo);
            }
        }

        private void OnError(Exception exception)
        {
			if (onError != null)
            {
				Debug.LogErrorFormat("[Download Error] \n{0}", exception);

				onError.OnNext(exception);
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
