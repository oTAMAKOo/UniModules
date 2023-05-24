
#if ENABLE_CRIWARE_FILESYSTEM

using UnityEngine;
using System;
using System.Threading;
using System.IO;
using System.Linq;
using CriWare;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Net;
using Modules.ExternalAssets;

namespace Modules.CriWare
{
    public sealed partial class CriAssetManager
    {
        public sealed class CriAssetInstall
        {
            //----- params -----

            // ダウンロード完了後にファイルがない場合のリトライ回数.
            private const int FileMissingMaxRetryCount = 3;

            // リトライ開始までのディレイ.
            private const int RetryDelaySeconds = 10;

            //----- field -----

            //----- property -----

            public AssetInfo AssetInfo { get; private set; }

            public CriFsWebInstaller Installer { get; private set; }

            public IObservable<CriAssetInstall> Task { get; private set; }

            //----- method -----

            public CriAssetInstall(string installPath, AssetInfo assetInfo, IProgress<DownloadProgressInfo> progress, CancellationToken cancelToken)
            {
                AssetInfo = assetInfo;

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

                Task = UniTask.Defer(() => Install(assetInfo, filePath, progress, cancelToken))
                    .ToObservable()
                    .Select(_ => this)
                    .Share();
            }

            private CriFsWebInstaller GetInstaller()
            {
                var installers = Instance.installers;
                var installQueueing = Instance.installQueueing;

                // 解放中止.

                Instance.CancelReleaseInstallers();
                
                // 使用中のインストーラ.

                CriFsWebInstaller[] installersInUse = null;

                lock (installQueueing)
                {
                    installersInUse = installQueueing
                        .Where(x => x.Value != null)
                        .Select(x => x.Value.Installer)
                        .ToArray();
                }

                // 未使用のインストーラを取得.

                CriFsWebInstaller installer = null;

                lock (installers)
                {
                    installer = installers.FirstOrDefault(x => installersInUse.All(y => x != y));

                    if (installer == null)
                    {
                        var numInstallers = Instance.numInstallers;

                        // 最大インストーラ数以下でインストーラが足りない時は生成.

                        if (installers.Count < numInstallers)
                        {
                            installer = new CriFsWebInstaller();

                            installers.Add(installer);
                        }
                    }
                }
                
                if (installer != null)
                {
                    installer.Stop();
                }

                return installer;
            }

            private async UniTask Install(AssetInfo assetInfo, string filePath, IProgress<DownloadProgressInfo> progress, CancellationToken cancelToken)
            {
                try
                {
                    var retryCount = 0;

                    var url = Instance.BuildDownloadUrl(assetInfo);

                    DownloadProgressInfo progressInfo = null;

                    if (progress != null)
                    {
                        progressInfo = new DownloadProgressInfo(assetInfo);
                    }

                    while (true)
                    {
                        // 同時インストール待ち.

                        while (true)
                        {
                            if (cancelToken.IsCancellationRequested) { return; }

                            // 未使用のインストーラを取得.
                            Installer = GetInstaller();

                            if (Installer != null) { break; }

                            await UniTask.NextFrame(CancellationToken.None);
                        }

                        // ネットワーク接続待ち.

                        await NetworkConnection.WaitNetworkReachable(cancelToken);

                        // ダウンロード.

                        if (Installer != null)
                        {
                            Installer.Copy(url, filePath);

                            // ダウンロード待ち.

                            CriFsWebInstaller.StatusInfo statusInfo;

                            while (true)
                            {
                                if (cancelToken.IsCancellationRequested) { return; }

                                statusInfo = Installer.GetStatusInfo();

                                if (progress != null)
                                {
                                    progressInfo.SetProgress((float)statusInfo.receivedSize / statusInfo.contentsSize);

                                    progress.Report(progressInfo);
                                }

                                if (statusInfo.status != CriFsWebInstaller.Status.Busy) { break; }

                                await UniTask.NextFrame(CancellationToken.None);
                            }
                            
                            if (statusInfo.error != CriFsWebInstaller.Error.None)
                            {
                                if (File.Exists(filePath))
                                {
                                    File.Delete(filePath);
                                }
                            }

                            if (File.Exists(filePath)){ break; }

                            // ファイルが存在しない時はリトライ.
                            if (FileMissingMaxRetryCount <= retryCount)
                            {
                                var message = $"URL: {url}\nFile: {filePath}\n";

                                if (statusInfo.error != CriFsWebInstaller.Error.None)
                                {
                                    message += $"CriFsWebInstaller.Error: {statusInfo.error}";
                                }

                                throw new FileNotFoundException(message);
                            }

                            Installer.Stop();

                            retryCount++;

                            await UniTask.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), cancellationToken: cancelToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    /* Canceled */
                }
                finally
                {
                    if (Installer != null)
                    {
                        Installer.Stop();
                    }
                }
            }
        }
    }
}

#endif