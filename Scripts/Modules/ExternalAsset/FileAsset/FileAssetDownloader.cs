
using UnityEngine;
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Extensions;
using Modules.AssetBundles;
using Modules.Net.WebDownload;
using Modules.Net.WebRequest;

namespace Modules.ExternalAssets
{
    public sealed class FileAssetDownLoader : FileDownLoader<DownloadRequest>
    {
        //----- params -----

        //----- field -----

        private Subject<Exception> onError = null;
        private Subject<string> onTimeout = null;

        //----- property -----

        //----- method -----

        public async UniTask Download(string installPath, AssetInfo assetInfo, string url, IProgress<DownloadProgressInfo> progress = null, CancellationToken cancelToken = default)
        {
            // ExternalAsset.GetFilePath経由でハッシュベースの命名に統一.
            var filePath = ExternalAsset.Instance.GetFilePath(installPath, assetInfo);

            if (string.IsNullOrEmpty(filePath)){ return; }

            // 中断時に壊れたファイルが正規の名前で残らないよう一時ファイルに書き出す.
            var tempFilePath = filePath + AssetBundleManager.TempPackageExtension;

            AssetBundleManager.ForceDeleteFile(tempFilePath);

            var directory = Directory.GetParent(tempFilePath);

            if (!directory.Exists)
            {
                directory.Create();
            }

            IProgress<float> progressReceiver = null;

            if (progress != null)
            {
                var progressInfo = new DownloadProgressInfo(assetInfo);

                void OnProgressUpdate(float value)
                {
                    progressInfo.SetProgress(value);

                    progress.Report(progressInfo);
                }

                progressReceiver = new Progress<float>(OnProgressUpdate);
            }

            var downloadRequest = SetupDownloadRequest(url, tempFilePath);

            await Download(downloadRequest, progressReceiver, cancelToken);

            if (cancelToken.IsCancellationRequested){ return; }

            // DL完了後にリネーム.

            AssetBundleManager.ForceDeleteFile(filePath);

            File.Move(tempFilePath, filePath);
        }

        protected override void OnComplete(DownloadRequest downloadRequest, double totalMilliseconds) { }

        protected override UniTask<RequestErrorHandle> OnError(DownloadRequest downloadRequest, Exception ex, CancellationToken cancelToken = default)
        {
            var type = ex.GetType();

            if (type == typeof(TimeoutException))
            {
                if (onTimeout != null)
                {
                    onTimeout.OnNext(downloadRequest.Url);
                }
                else
                {
                    Debug.LogErrorFormat("DownloadRequest Timeout \n\n[URL]\n{0}\n\n[Exception]\n{1}\n", downloadRequest.Url, ex.StackTrace);
                }
            }
            else if (type == typeof(UnityWebRequestErrorException) && ex is UnityWebRequestErrorException)
            {
                if (onError != null)
                {
                    onError.OnNext(ex);
                }
                else
                {
                    var exception = (UnityWebRequestErrorException)ex;
                    var errorMessage = exception.RawErrorMessage;

                    Debug.LogErrorFormat("DownloadRequest Error : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", errorMessage, downloadRequest.Url, ex.StackTrace);
                }
            }
            else
            {
                if (onError != null)
                {
                    onError.OnNext(ex);
                }
                else
                {
                    Debug.LogErrorFormat("DownloadRequest UnknownError : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", ex.Message, downloadRequest.Url, ex.StackTrace);
                }
            }

            return UniTask.FromResult(RequestErrorHandle.Retry);
        }

        protected override void OnRetryLimit(DownloadRequest downloadRequest)
        {
            var ex = new Exception($"DownloadRequest RetryLimit : [URL]\n{downloadRequest.Url}");

            if (onError != null)
            {
                onError.OnNext(ex);
            }
        }

        public Observable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }

        public Observable<string> OnTimeoutAsObservable()
        {
            return onTimeout ?? (onTimeout = new Subject<string>());
        }
    }
}