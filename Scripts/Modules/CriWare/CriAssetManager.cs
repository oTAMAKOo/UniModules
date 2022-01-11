
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC
﻿﻿﻿
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CriWare;
using UniRx;
using Extensions;
using Modules.Devkit.Console;
using Modules.ExternalResource;

namespace Modules.CriWare
{
    public sealed class CriAssetManager : Singleton<CriAssetManager>
    {
        //----- params -----

        #if ENABLE_CRIWARE_FILESYSTEM

        private sealed class CriAssetInstall
        {
            private static int installCount = 0;

            public AssetInfo AssetInfo { get; private set; }
            public CriFsWebInstaller Installer { get; private set; }
            public IObservable<CriAssetInstall> Task { get; private set; }

            public CriAssetInstall(AssetInfo assetInfo, IProgress<float> progress = null)
            {
                AssetInfo = assetInfo;

                var downloadUrl = Instance.BuildDownloadUrl(assetInfo);
                var installPath = Instance.BuildFilePath(assetInfo);

                var directory = Path.GetDirectoryName(installPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(installPath))
                {
                    File.Delete(installPath);
                }

                Task = Observable.FromMicroCoroutine(() => Install(downloadUrl, installPath, progress))
                    .Select(_ => this)
                    .Share();
            }

            private IEnumerator Install(string downloadUrl, string installPath, IProgress<float> progress = null)
            {
                var numInstallers = Instance.numInstallers;

                // 同時インストール数待ち.
                while (numInstallers <= installCount)
                {
                    yield return null;
                }

                installCount++;

                using (Installer = new CriFsWebInstaller())
                {
                    Installer.Copy(downloadUrl, installPath);

                    CriFsWebInstaller.StatusInfo statusInfo;

                    while (true)
                    {
                        statusInfo = Installer.GetStatusInfo();

                        if (progress != null)
                        {
                            progress.Report((float)statusInfo.receivedSize / statusInfo.contentsSize);
                        }

                        if (statusInfo.status != CriFsWebInstaller.Status.Busy)
                        {
                            break;
                        }

                        yield return null;
                    }

                    if (statusInfo.error != CriFsWebInstaller.Error.None)
                    {
                        throw new Exception(string.Format("[Download Error] {0}\n{1}", AssetInfo.ResourcePath, statusInfo.error));
                    }
                }

                installCount--;
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

        // インストール先.
        private string installPath = null;

        // ダウンロード元URL.
        private string remoteUrl = null;
        private string versionHash = null;

        // シュミュレートモードか.
        private bool simulateMode = false;

        // ローカルモードか.
        private bool localMode = false;

        // フォルダディレクトリ.
        private string sourceDir = null;

        #if ENABLE_CRIWARE_FILESYSTEM

        // ダウンロード待ち.
        private Dictionary<string, CriAssetInstall> installQueueing = null;

        #endif

        // 同時インストール数.
        private uint numInstallers = 0;

        // イベント通知.
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private bool isInitialized = false;

        //----- property -----

        //----- method -----

        private CriAssetManager() { }

        public void Initialize(string sourceDir, uint numInstallers, bool simulateMode = false)
        {
            if (isInitialized) { return; }

            this.sourceDir = sourceDir;
            this.simulateMode = Application.isEditor && simulateMode;
            this.numInstallers = numInstallers;

            #if ENABLE_CRIWARE_FILESYSTEM

            installQueueing = new Dictionary<string, CriAssetInstall>();

            //------ CriInstaller初期化 ------

            var moduleConfig = CriFsWebInstaller.defaultModuleConfig;

            // 同時インストール数.
            moduleConfig.numInstallers = numInstallers;
            // タイムアウト時間.
            moduleConfig.inactiveTimeoutSec = (uint)TimeoutLimit.TotalSeconds;

            CriFsWebInstaller.InitializeModule(moduleConfig);

            Observable.EveryUpdate()
                .Subscribe(_ => CriFsWebInstaller.ExecuteMain())
                .AddTo(Disposable);

            #endif

            isInitialized = true;
        }

        protected override void OnRelease()
        {
            if (isInitialized)
            {
                #if ENABLE_CRIWARE_FILESYSTEM

                foreach (var item in installQueueing.Values)
                {
                    item.Installer.Stop();
                    item.Installer.Dispose();
                }

                installQueueing.Clear();

                CriFsWebInstaller.FinalizeModule();

                #endif
            }
        }

        /// <summary> ローカルモード設定.
        /// <see cref="installPath"/>のファイルからアセットを取得
        /// </summary>
        public void SetLocalMode(bool localMode)
        {
            this.localMode = localMode;
        }

        /// <summary> 保存先ディレクトリ設定. </summary>
        public void SetInstallDirectory(string installDirectory)
        {
            installPath = installDirectory;
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

            CleanUnuseCache();
        }

        #if ENABLE_CRIWARE_FILESYSTEM

        /// <summary>
        /// 指定されたアセットを更新.
        /// </summary>
        public IObservable<Unit> UpdateCriAsset(AssetInfo assetInfo, IProgress<float> progress = null)
        {
            if (simulateMode) { return Observable.ReturnUnit(); }

            if (localMode) { return Observable.ReturnUnit(); }

            var resourcePath = assetInfo.ResourcePath;
            var extension = Path.GetExtension(assetInfo.ResourcePath);

            if (extension == CriAssetDefinition.AwbExtension)
            {
                return Observable.ReturnUnit();
            }

            var installList = new List<CriAssetInstall>();

            CriAssetInstall install = null;

            if (extension == CriAssetDefinition.AcbExtension)
            {
                // Awbの拡張子でマニフェストを検索して存在したら一緒にダウンロード.
                var awbAssetPath = Path.ChangeExtension(resourcePath, CriAssetDefinition.AwbExtension);

                var awbAssetInfo = manifest.GetAssetInfo(awbAssetPath);

                //------- Acb ------- 

                //インストールの進行度はAwbがない場合に渡す.
                install = GetCriAssetInstall(assetInfo, awbAssetInfo == null ? progress : null);

                installList.Add(install);

                //------- Awb -------

                if (awbAssetInfo != null)
                {
                    install = GetCriAssetInstall(awbAssetInfo, progress);

                    installList.Add(install);
                }
            }
            else if (extension == CriAssetDefinition.UsmExtension)
            {
                //------- Usm -------

                install = GetCriAssetInstall(assetInfo, progress);

                installList.Add(install);
            }

            if (installList.IsEmpty())
            {
                Debug.LogErrorFormat("UpdateCriAsset Error.\n{0}", assetInfo.ResourcePath);
                return Observable.ReturnUnit();
            }

            return installList
                .Select(x => x.Task)
                .WhenAll()
                .Timeout(TimeoutLimit)
                .OnErrorRetry((TimeoutException ex) => OnTimeout(assetInfo, ex), RetryCount, RetryDelaySeconds)
                .DoOnError(ex => OnError(ex))
                .Finally(() => installList.ForEach(item => RemoveInternalQueue(item)))
                .AsUnitObservable();
        }

        private CriAssetInstall GetCriAssetInstall(AssetInfo assetInfo, IProgress<float> progress)
        {
            var install = installQueueing.GetValueOrDefault(assetInfo.ResourcePath);

            if (install != null) { return install; }

            install = new CriAssetInstall(assetInfo, progress);

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
                item.Installer.Stop();
                item.Installer.Dispose();

                installQueueing.Remove(resourcePath);
            }
        }

        #endif

        /// <summary>
        /// マニフェストファイルに存在しないキャッシュファイルを破棄.
        /// </summary>
        private void CleanUnuseCache()
        {
            if (simulateMode) { return; }

            if (manifest == null) { return; }

            var installDir = BuildFilePath(null);

            if (string.IsNullOrEmpty(installDir)) { return; }

            if (!Directory.Exists(installDir)) { return; }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var builder = new StringBuilder();
            var directory = Path.GetDirectoryName(installDir);

            if (Directory.Exists(directory))
            {
                var cacheFiles = Directory.GetFiles(installDir, "*", SearchOption.AllDirectories);

                var managedFiles = manifest.GetAssetInfos()
                    .Select(x => BuildFilePath(x))
                    .Select(x => PathUtility.ConvertPathSeparator(x))
                    .Distinct()
                    .ToHashSet();

                var deleteFilePaths = cacheFiles
                    .Select(x => PathUtility.ConvertPathSeparator(x))
                    .Where(x =>
                       {
                           var extension = Path.GetExtension(x);

                           return CriAssetDefinition.AssetAllExtensions.Any(y => y == extension);
                       })
                    .Where(x => !managedFiles.Contains(x))
                    .ToArray();

                foreach (var deleteFilePath in deleteFilePaths)
                {
                    if (File.Exists(deleteFilePath))
                    {
                        File.SetAttributes(deleteFilePath, FileAttributes.Normal);
                        File.Delete(deleteFilePath);
                    }

                    var versionFilePath = deleteFilePath + AssetInfoManifest.VersionFileExtension;

                    if (File.Exists(versionFilePath))
                    {
                        File.SetAttributes(versionFilePath, FileAttributes.Normal);
                        File.Delete(versionFilePath);
                    }

                    builder.AppendLine(deleteFilePath);
                }

                var deleteDirectorys = DirectoryUtility.DeleteEmpty(installDir);

                deleteDirectorys.ForEach(x => builder.AppendLine(x));

                sw.Stop();

                var log = builder.ToString();

                if (!string.IsNullOrEmpty(log))
                {
                    UnityConsole.Info("Delete unuse cached criassets ({0}ms)\n{1}", sw.Elapsed.TotalMilliseconds, log);
                }
            }
        }

        public string BuildDownloadUrl(AssetInfo assetInfo)
        {
            var platformName = PlatformUtility.GetPlatformTypeName();

            var url = PathUtility.Combine(new string[] { remoteUrl, platformName, versionHash, assetInfo.FileName });

            return string.Format("{0}?v={1}", url, assetInfo.Hash);
        }

        public string BuildFilePath(AssetInfo assetInfo)
        {
            var path = installPath;

            if (assetInfo != null)
            {
                path = PathUtility.Combine(installPath, assetInfo.FileName);
            }

            return path;
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
