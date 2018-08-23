﻿﻿
#if ENABLE_CRIWARE
﻿﻿﻿
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.ExternalResource;

namespace Modules.CriWare
{
    public partial class CriAssetManager : Singleton<CriAssetManager>
    {
        //----- params -----

        private class CriAssetInstall
        {
            public AssetInfo AssetInfo { get; private set; }
            public CriFsInstallRequest Request { get; private set; }
            public IObservable<CriAssetInstall> Task { get; private set; }

            public CriAssetInstall(AssetInfo assetInfo, CriFsInstallRequest request, IProgress<float> progress = null)
            {
                AssetInfo = assetInfo;
                Request = request;

                Task = Observable.FromMicroCoroutine(() => Install(request, progress))
                    .Select(_ => this)
                    .Share();
            }

            private IEnumerator Install(CriFsInstallRequest request, IProgress<float> progress = null)
            {
                while (!request.isDone)
                {
                    if (request.isDisposed) { break; }

                    if (progress != null)
                    {
                        progress.Report(request.progress);
                    }

                    yield return null;
                }

                if (request.error != null)
                {
                    throw new Exception(string.Format("[Download Error] {0}\n{1}", AssetInfo.ResourcesPath, request.error));
                }
            }
        }

        // タイムアウトまでの時間.
        private readonly TimeSpan TimeoutLimit = TimeSpan.FromSeconds(180f);

        // リトライする回数.
        private readonly int RetryCount = 3;

        // リトライするまでの時間(秒).
        private readonly TimeSpan RetryDelaySeconds = TimeSpan.FromSeconds(2f);

        //----- field -----

        // ダウンロード元URL.
        private string remoteUrl = null;

        // インストール先.
        private string installDir = null;

        // シュミュレートモードか.
        private bool simulateMode = false;

        // フォルダディレクトリ.
        private string sourceDir = null;

        // ダウンロード待ち.
        private Dictionary<string, CriAssetInstall> installQueueing = null;

        // アセット管理.
        private AssetInfoManifest manifest = null;
        
        // イベント通知.
        private Subject<string> onTimeOut = null;
        private Subject<Exception> onError = null;

        private bool isInitialized = false;

        //----- property -----

        //----- method -----

        public void Initialize(string sourceDir, bool simulateMode = false)
        {
            if (isInitialized) { return; }

            this.sourceDir = sourceDir;
            this.simulateMode = Application.isEditor && simulateMode;

            installQueueing = new Dictionary<string, CriAssetInstall>();

            installDir = GetInstallDirectory();

            isInitialized = true;
        }

        /// <summary>
        /// URLを設定.
        /// </summary>
        public void SetUrl(string remoteUrl)
        {
            this.remoteUrl = PathUtility.Combine(new string[] { remoteUrl, UnityPathUtility.GetPlatformName(), CriAssetDefinition.CriAssetFolder });
        }

        public void SetManifest(AssetInfoManifest manifest)
        {
            this.manifest = manifest;

            CleanUnuseCache();
        }

        /// <summary>
        /// 全てのキャッシュを破棄.
        /// </summary>
        public static void CleanCache()
        {
            var installDir = GetInstallDirectory();

            if (Directory.Exists(installDir))
            {
                DirectoryUtility.Clean(installDir);

                // 一旦削除するので再度生成.
                Directory.CreateDirectory(installDir);
            }
        }

        /// <summary>
        /// 指定されたアセットを更新.
        /// </summary>
        public IObservable<Unit> UpdateCriAsset(string resourcesPath, IProgress<float> progress = null)
        {
            if (simulateMode) { return Observable.ReturnUnit(); }

            if (Path.GetExtension(resourcesPath) == CriAssetDefinition.AwbExtension) { return Observable.ReturnUnit(); }

            var installList = new List<CriAssetInstall>();

            var extension = Path.GetExtension(resourcesPath);

            var assetInfo = manifest.GetAssetInfo(resourcesPath);

            if (assetInfo == null)
            {
                OnError(new FileNotFoundException(string.Format("File NotFound.\n[{0}]", resourcesPath)));
                return Observable.ReturnUnit();
            }

            CriAssetInstall install = null;

            if (extension == CriAssetDefinition.AcbExtension)
            {
                // Awbの拡張子でマニフェストを検索して存在したら一緒にダウンロード.
                var awbAssetPath = Path.ChangeExtension(resourcesPath, CriAssetDefinition.AwbExtension);

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
                Debug.LogErrorFormat("UpdateCriAsset Error.\n{0}", assetInfo.ResourcesPath);
                return Observable.ReturnUnit();
            }

            return installList
                .Select(x => x.Task)
                .WhenAll()
                .Timeout(TimeoutLimit)
                .OnErrorRetry((TimeoutException ex) => OnTimeout(resourcesPath, ex), RetryCount, RetryDelaySeconds)
                .DoOnError(ex => OnError(ex))
                .Finally(() => installList.ForEach(item => RemoveInternalQueue(item)))
                .AsUnitObservable();
        }

        private CriAssetInstall GetCriAssetInstall(AssetInfo assetInfo, IProgress<float> progress)
        {
            var resourcePath = UnityPathUtility.GetLocalPath(assetInfo.ResourcesPath, sourceDir);
            var downloadUrl = PathUtility.Combine(remoteUrl, resourcePath);
            var installPath = PathUtility.Combine(installDir, resourcePath);

            var install = installQueueing.GetValueOrDefault(assetInfo.ResourcesPath);

            if (install != null) { return install; }

            var directory = Path.GetDirectoryName(installPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var request = CriFsUtility.Install(downloadUrl, installPath);

            install = new CriAssetInstall(assetInfo, request, progress);

            installQueueing[assetInfo.ResourcesPath] = install;

            return install;
        }

        private void RemoveInternalQueue(CriAssetInstall install)
        {
            if (install == null) { return; }

            if (install.AssetInfo == null) { return; }

            var resourcesPath = install.AssetInfo.ResourcesPath;

            var item = installQueueing.GetValueOrDefault(resourcesPath);

            if (item != null)
            {
                item.Request.Stop();
                item.Request.Dispose();

                installQueueing.Remove(resourcesPath);
            }
        }

        /// <summary>
        /// マニフェストファイルに存在しないキャッシュファイルを破棄.
        /// </summary>
        private void CleanUnuseCache()
        {
            if (simulateMode) { return; }

            if (manifest == null) { return; }

            if (string.IsNullOrEmpty(installDir)) { return; }

            var builder = new StringBuilder();
            var directory = Path.GetDirectoryName(installDir);

            if (Directory.Exists(directory))
            {
                var cacheFiles = Directory.GetFiles(installDir, "*", SearchOption.AllDirectories);

                var managedFiles = manifest.GetAssetInfos()
                    .Select(x => UnityPathUtility.GetLocalPath(x.ResourcesPath, sourceDir))
                    .Select(x => PathUtility.Combine(installDir, x))
                    .Distinct()
                    .ToHashSet();

                var targets = cacheFiles
                    .Select(x => PathUtility.ConvertPathSeparator(x))
                    .Where(x => !managedFiles.Contains(x))
                    .ToArray();

                foreach (var target in targets)
                {
                    if (!File.Exists(target)) { continue; }

                    File.SetAttributes(target, FileAttributes.Normal);
                    File.Delete(target);

                    builder.AppendLine(target);
                }

                var deleteDirectorys = DirectoryUtility.DeleteEmpty(installDir);

                deleteDirectorys.ForEach(x => builder.AppendLine(x));

                if (!string.IsNullOrEmpty(builder.ToString()))
                {
                    Debug.LogFormat("CriAsset CleanUnuseCache:\n{0}", builder.ToString());
                }
            }
        }

        public static string GetInstallDirectory()
        {
            return PathUtility.Combine(UnityPathUtility.GetInstallPath(), CriAssetDefinition.CriAssetFolder) + PathUtility.PathSeparator;
        }

        private void OnTimeout(string url, Exception exception)
        {
            Debug.LogErrorFormat("[Download Timeout] \n{0}", exception);

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(url);
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
        public IObservable<string> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<string>());
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