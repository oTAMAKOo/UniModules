﻿
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
    public sealed class CriAssetManager : Singleton<CriAssetManager>
    {
        //----- params -----

        #if ENABLE_CRIWARE_FILESYSTEM

        private sealed class CriAssetInstall
        {
            public AssetInfo AssetInfo { get; private set; }
            public CriFsWebInstaller Installer { get; private set; }
            public IObservable<CriAssetInstall> Task { get; private set; }

            public CriAssetInstall(string installPath, AssetInfo assetInfo, IProgress<float> progress = null)
            {
                AssetInfo = assetInfo;

                var downloadUrl = Instance.BuildDownloadUrl(assetInfo);
                var filePath = Instance.GetFilePath(installPath, assetInfo);

                var directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                Task = ObservableEx.FromUniTask(cancelToken => Install(downloadUrl, filePath, progress, cancelToken))
                    .Select(_ => this)
                    .Share();
            }

            private CriFsWebInstaller GetInstaller()
            {
                var installers = Instance.installers;
                var numInstallers = Instance.numInstallers;

                CriFsWebInstaller installer = null;

                // 未使用のインストーラを取得.
                installer = installers.FirstOrDefault(x =>
                {
                    var statusInfo = x.GetStatusInfo();

                    return statusInfo.status != CriFsWebInstaller.Status.Busy;
                });

                if (installer == null)
                {
                    // 最大インストーラ数以下でインストーラが足りない時は生成.
                    if (installers.Count < numInstallers)
                    {
                        installer = new CriFsWebInstaller();

                        installers.Add(installer);
                    }
                }

                if (installer != null)
                {
                    installer.Stop();
                }

                return installer;
            }

            private async UniTask Install(string downloadUrl, string filePath, IProgress<float> progress, CancellationToken cancelToken)
            {
                // 同時インストール数待ち.
                while (true)
                {
                    // 未使用のインストーラを取得.
                    Installer = GetInstaller();

                    if (Installer != null){ break; }

                    if (cancelToken.IsCancellationRequested){ return; }

                    await UniTask.NextFrame(cancelToken);
				}

				if (cancelToken.IsCancellationRequested){ return; }
                
                Installer.Copy(downloadUrl, filePath);

                CriFsWebInstaller.StatusInfo statusInfo;

                while (true)
                {
                    statusInfo = Installer.GetStatusInfo();

                    if (progress != null)
                    {
                        progress.Report((float)statusInfo.receivedSize / statusInfo.contentsSize);
                    }

                    if (statusInfo.status != CriFsWebInstaller.Status.Busy) { break; }

                    if (cancelToken.IsCancellationRequested){ break; }

                    await UniTask.NextFrame(cancelToken);
                }

                if (statusInfo.error != CriFsWebInstaller.Error.None)
                {
                    throw new Exception(string.Format("[Download Error] {0}\n{1}", AssetInfo.ResourcePath, statusInfo.error));
                }
            }
        }

        // タイムアウトまでの時間.
        private readonly TimeSpan TimeoutLimit = TimeSpan.FromSeconds(180f);

        // リトライする回数.
        private readonly int RetryCount = 3;

        // リトライするまでの時間(秒).
        private readonly TimeSpan RetryDelaySeconds = TimeSpan.FromSeconds(2f);

        #endif

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

        /// <summary>
        /// 指定されたアセットを更新.
        /// </summary>
        public async UniTask UpdateCriAsset(string installPath, AssetInfo assetInfo, CancellationToken cancelToken, IProgress<float> progress = null)
        {
            if (simulateMode) { return; }

            if (localMode) { return; }

            var resourcePath = assetInfo.ResourcePath;
            var extension = Path.GetExtension(assetInfo.ResourcePath);

            if (extension == CriAssetDefinition.AwbExtension) { return; }

            var installList = new List<CriAssetInstall>();

            CriAssetInstall install = null;

            if (extension == CriAssetDefinition.AcbExtension)
            {
                // Awbの拡張子でマニフェストを検索して存在したら一緒にダウンロード.
                var awbAssetPath = Path.ChangeExtension(resourcePath, CriAssetDefinition.AwbExtension);

                var awbAssetInfo = manifest.GetAssetInfo(awbAssetPath);

                //------- Acb ------- 

                //インストールの進行度はAwbがない場合に渡す.
                install = GetCriAssetInstall(installPath, assetInfo, awbAssetInfo == null ? progress : null);

                installList.Add(install);

                //------- Awb -------

                if (awbAssetInfo != null)
                {
                    install = GetCriAssetInstall(installPath, awbAssetInfo, progress);

                    installList.Add(install);
                }
            }
            else if (extension == CriAssetDefinition.UsmExtension)
            {
                //------- Usm -------

                install = GetCriAssetInstall(installPath, assetInfo, progress);

                installList.Add(install);
            }

            if (installList.IsEmpty())
            {
                Debug.LogErrorFormat("UpdateCriAsset Error.\n{0}", assetInfo.ResourcePath);
                return;
            }

            await installList
                .Select(x => x.Task)
                .WhenAll()
                .Timeout(TimeoutLimit)
                .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds)
                .DoOnError(ex => OnError(ex))
                .Finally(() => installList.ForEach(item => RemoveInternalQueue(item)))
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

            return string.Format("{0}?v={1}", url, assetInfo.Hash);
        }

        public bool IsCriAsset(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            return CriAssetDefinition.AssetAllExtensions.Any(y => y == extension);
        }

        public string GetFilePath(string installPath, AssetInfo assetInfo)
        {
			if (assetInfo == null){ return null; }
            
            return PathUtility.Combine(installPath, assetInfo.FileName);
        }

        private void OnTimeout(AssetInfo assetInfo, Exception exception)
        {
            Debug.LogErrorFormat("[Download Timeout] \n{0}", exception);

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
            }
        }

        private void OnError(Exception exception)
        {
            Debug.LogErrorFormat("[Download Error] \n{0}", exception);

            if (onError != null)
            {
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
