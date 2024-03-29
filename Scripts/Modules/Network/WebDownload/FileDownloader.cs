
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.Net.WebDownload
{
    public abstract class FileDownLoader<TDownloadRequest> : LifetimeDisposable where TDownloadRequest : DownloadRequest, new()
    {
        //----- params -----

        private const uint DefaultMaxDownloadCount = 5;

        protected enum RequestErrorHandle
        {
            Retry,
            Cancel,
        }

        private sealed class DownloadInfo
        {
            public TDownloadRequest Request { get; private set; }
            public IObservable<bool> Task { get; private set; }

            public DownloadInfo(TDownloadRequest request, IObservable<bool> task)
            {
                Request = request;
                Task = task;
            }
        }

        //----- field -----

        private Dictionary<string, DownloadInfo> downloadItems = null;

        private List<TDownloadRequest> downloading = null;

        private bool initialized = false;

        //----- property -----

        /// <summary> 接続先URL. </summary>
        public string ServerUrl { get; private set; }

        /// <summary> 同時ダウンロード数. </summary>
        public uint MaxDownloadCount { get; private set; }

        /// <summary> リトライ回数. </summary>
        public int RetryCount { get; private set; }

        /// <summary> リトライするまでの時間(秒). </summary>
        public float RetryDelaySeconds { get; private set; }

        //----- method -----

        public void Initialize(int retryCount = 3, float retryDelaySeconds = 2)
        {
            if (initialized) { return; }

            downloadItems = new Dictionary<string, DownloadInfo>();
            downloading = new List<TDownloadRequest>();

            RetryCount = retryCount;
            RetryDelaySeconds = retryDelaySeconds;

            SetMaxDownloadCount(DefaultMaxDownloadCount);

            OnInitialize();

            initialized = true;
        }

        public void SetMaxDownloadCount(uint maxDownloadCount)
        {
            MaxDownloadCount = maxDownloadCount;
        }

        public void SetServerUrl(string serverUrl)
        {
            ServerUrl = serverUrl;
        }

        protected TDownloadRequest SetupDownloadRequest(string url, string filePath)
        {
            var downloadRequest = new TDownloadRequest();

            var downloadUrl = PathUtility.Combine(ServerUrl, url);

            downloadRequest.Initialize(downloadUrl, filePath);

            return downloadRequest;
        }

        /// <summary>
        /// 指定されたURLからGetでデータを取得.
        /// </summary>
        protected async UniTask<bool> Download(TDownloadRequest downloadRequest, IProgress<float> progress = null, CancellationToken cancelToken = default)
        {
            if (downloadRequest == null){ return false; }

            var result = false;

            try
            {
                IObservable<bool> observable = null;

                var url = downloadRequest.Url;

                // 既にダウンロードキューに入っている場合は既存のObservableを返す.
                if (downloadItems.ContainsKey(url))
                {
                    observable = downloadItems[url].Task;
                }
                else
                {
                    observable = ObservableEx.FromUniTask(token => SendRequestInternal(downloadRequest, progress, token)).Share();

                    var downloadInfo = new DownloadInfo(downloadRequest, observable);

                    downloadItems.Add(url, downloadInfo);
                }

                result = await observable.ToUniTask(cancellationToken: cancelToken);
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }

            return result;
        }

        /// <summary> 通信処理が同時に実行されないようにキューイング. </summary>
        private async UniTask WaitQueueingRequest(TDownloadRequest downloadRequest, CancellationToken cancelToken)
        {
            var url = downloadRequest.Url;

            while (true)
            {
                if (cancelToken.IsCancellationRequested) { break; }

                // キューが空になっていた場合はキャンセル扱い.
                if (downloadItems.IsEmpty())
                {
                    if (downloadRequest != null)
                    {
                        downloadRequest.Cancel();
                    }
                    break;
                }

                // 最大同時ダウンロード数以下.
                if (downloading.Count <= MaxDownloadCount)
                {
                    // ダウンロード待ちの先頭を取得.
                    var first = downloadItems.FirstOrDefault(x => downloading.All(y => x.Key != y.Url));
                    
                    // キューの先頭が自身の場合待ち終了.
                    if (!first.IsDefault() && first.Key == url)
                    {
                        break;
                    }
                }

                await UniTask.NextFrame(CancellationToken.None);
            }
        }

        /// <summary> リクエスト制御 </summary>
        private async UniTask<bool> SendRequestInternal(TDownloadRequest downloadRequest, IProgress<float> progress, CancellationToken cancelToken)
        {
            var result = false;

            try
            {
                // ダウンロード待ちキュー.
                await WaitQueueingRequest(downloadRequest, cancelToken);

                // ネットワーク接続待ち.
                await NetworkConnection.WaitNetworkReachable(cancelToken);

                // ダウンロード中.
                downloading.Add(downloadRequest);

                var sw = System.Diagnostics.Stopwatch.StartNew();

                var retryCount = 0;

                // リクエスト実行.
                while (true)
                {
                    Exception exception = null;

                    try
                    {
                        await downloadRequest.Download(progress, cancelToken);

                        result = true;

                        break;
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    if (cancelToken.IsCancellationRequested){ break; }

                    //------ リトライ回数オーバー ------

                    if (RetryCount <= retryCount)
                    {
                        OnRetryLimit(downloadRequest);
                        break;
                    }

                    //------ 通信失敗 ------

                    // エラーハンドリングを待つ.

                    var requestErrorHandle = await OnError(downloadRequest, exception, cancelToken);

                    // リトライ.
                    if (requestErrorHandle == RequestErrorHandle.Retry) { retryCount++; }

                    // キャンセル時は通信終了.
                    if (requestErrorHandle == RequestErrorHandle.Cancel) { break; }

                    // リトライディレイ.
                    await UniTask.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), cancellationToken: cancelToken);
                }

                sw.Stop();

                // 正常終了.
                if (result)
                {
                    OnComplete(downloadRequest, sw.Elapsed.TotalMilliseconds);
                }
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
            finally
            {
                downloading.Remove(downloadRequest);

                downloadItems.Remove(downloadRequest.Url);
            }

            return result;
        }

        /// <summary> ダウンロード中の全リクエストを強制中止. </summary>
        protected void ForceCancelAll()
        {
            if (downloading.Any())
            {
                foreach (var request in downloading)
                {
                    if (request == null){ continue; }

                    request.Cancel();
                }

                downloading.Clear();
            }

            downloadItems.Clear();
        }

        /// <summary> 初期化処理. </summary>
        protected virtual void OnInitialize() { }

        /// <summary> 成功時イベント. </summary>
        protected abstract void OnComplete(TDownloadRequest downloadRequest, double totalMilliseconds);
        
        /// <summary> 通信エラー時イベント. </summary>
        protected abstract UniTask<RequestErrorHandle> OnError(TDownloadRequest downloadRequest, Exception ex, CancellationToken cancelToken = default);

        /// <summary> リトライ回数を超えた時のイベント. </summary>
        protected abstract void OnRetryLimit(TDownloadRequest downloadRequest);
    }
}
