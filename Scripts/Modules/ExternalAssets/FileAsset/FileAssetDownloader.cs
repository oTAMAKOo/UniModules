
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Extensions;
using Modules.AssetBundles;
using Modules.Devkit.Console;
using Modules.Network.WebDownload;
using Modules.Network.WebRequest;

namespace Modules.ExternalAssets
{
    public sealed class FileAssetDownLoader : FileDownLoader<DownloadRequest>
    {
        //----- params -----

        //----- field -----

        // 試行毎の最後のエラー.
        private Dictionary<DownloadRequest, Exception> lastErrors = null;

        private Subject<Exception> onError = null;
        private Subject<string> onTimeout = null;

        //----- property -----

        //----- method -----

        protected override void OnInitialize()
        {
            lastErrors = new Dictionary<DownloadRequest, Exception>();
        }

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

            var result = false;

            Exception lastError = null;

            try
            {
                result = await Download(downloadRequest, progressReceiver, cancelToken);
            }
            finally
            {
                lastError = lastErrors.GetValueOrDefault(downloadRequest);

                lastErrors.Remove(downloadRequest);
            }

            if (cancelToken.IsCancellationRequested){ return; }

            // リトライ上限に達した場合は失敗を送出する.
            if (!result)
            {
                throw new Exception($"File download failed.\nURL : {downloadRequest.Url}", lastError);
            }

            // DL完了後にリネーム.

            AssetBundleManager.ForceDeleteFile(filePath);

            File.Move(tempFilePath, filePath);
        }

        protected override void OnComplete(DownloadRequest downloadRequest, double totalMilliseconds) { }

        /// <summary> 試行毎の失敗はリトライし, 失敗の送出はリトライ上限到達時の Download で1回だけ行う. </summary>
        protected override UniTask<RequestErrorHandle> OnError(DownloadRequest downloadRequest, Exception ex, CancellationToken cancelToken = default)
        {
            lastErrors[downloadRequest] = ex;

            var webRequestError = ex as UnityWebRequestErrorException;

            var errorMessage = webRequestError != null ? webRequestError.RawErrorMessage : ex.Message;

            var message = $"Download retry : {errorMessage}\nURL : {downloadRequest.Url}";

            UnityConsole.Event(ExternalAsset.ConsoleEventName, ExternalAsset.ConsoleEventColor, message, LogType.Warning);

            return UniTask.FromResult(RequestErrorHandle.Retry);
        }

        protected override void OnRetryLimit(DownloadRequest downloadRequest) { }

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