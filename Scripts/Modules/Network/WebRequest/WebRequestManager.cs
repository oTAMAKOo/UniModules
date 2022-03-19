
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensions;

namespace Modules.Net.WebRequest
{
    public enum DataFormat
    {
        Json,
        MessagePack,
    }

    public abstract class WebRequestManager<TInstance, TWebRequest> : Singleton<TInstance>
        where TInstance : WebRequestManager<TInstance, TWebRequest> 
        where TWebRequest : class, IWebRequestClient, new()
    {
        //----- params -----

        protected enum RequestErrorHandle
        {
            None = 0,

            Retry,
            Cancel,
        }

        //----- field -----

        private TWebRequest current = null;

        private Queue<TWebRequest> requestQueue = null;
        
        private CancellationTokenSource cancellationTokenSource = null;

        //----- property -----

        /// <summary> 接続先URL. </summary>
        public string HostUrl { get; private set; }

        /// <summary> 送受信データの圧縮. </summary>
        public bool Compress { get; private set; }

        /// <summary> データ内容フォーマット. </summary>
        public DataFormat Format { get; private set; }

        /// <summary> ヘッダー情報 [key, (encrypted, value)]. </summary>
        public IDictionary<string, Tuple<bool, string>> Headers { get; private set; }

        /// <summary> リトライ回数. </summary>
        public int RetryCount { get; private set; }

        /// <summary> リトライするまでの時間(秒). </summary>
        public float RetryDelaySeconds { get; private set; }

        //----- method -----

        protected WebRequestManager()
        {
            requestQueue = new Queue<TWebRequest>();
            Headers = new Dictionary<string, Tuple<bool, string>>();
        }

        public virtual void Initialize(string hostUrl, bool compress = true, DataFormat format = DataFormat.MessagePack, int retryCount = 3, float retryDelaySeconds = 2)
        {
            HostUrl = hostUrl;
            Compress = compress;
            Format = format;
            RetryCount = retryCount;
            RetryDelaySeconds = retryDelaySeconds;
        }

        /// <summary> リソースの取得. </summary>
        protected async Task<TResult> Get<TResult>(TWebRequest webRequest, IProgress<float> progress = null) where TResult : class
        {
            var requestTask = new Func<Task<TResult>>(async () => await webRequest.Get<TResult>(progress));

            var request = await Request(webRequest, requestTask);

            return request.Result;
        }

        /// <summary> リソースの作成、追加. </summary>
        protected async Task<TResult> Post<TResult, TContent>(TWebRequest webRequest, TContent content, IProgress<float> progress = null) where TResult : class
        {
            var requestTask = new Func<Task<TResult>>(async () => await webRequest.Post<TResult, TContent>(content, progress));

            var request = await Request(webRequest, requestTask);

            return request.Result;
        }

        /// <summary> リソースの更新、作成. </summary>
        protected async Task<TResult> Put<TResult, TContent>(TWebRequest webRequest, TContent content, IProgress<float> progress = null) where TResult : class
        {
            var requestTask = new Func<Task<TResult>>(async () => await webRequest.Put<TResult, TContent>(content, progress));

            var request = await Request(webRequest, requestTask);

            return request.Result;
        }

        /// <summary> リソースの部分更新. </summary>
        protected async Task<TResult> Patch<TResult, TContent>(TWebRequest webRequest, TContent content, IProgress<float> progress = null) where TResult : class
        {
            var requestTask = new Func<Task<TResult>>(async () => await webRequest.Patch<TResult, TContent>(content, progress));

            var request = await Request(webRequest, requestTask);

            return request.Result;
        }

        /// <summary> リソースの削除. </summary>
        protected async Task<TResult> Delete<TResult>(TWebRequest webRequest, IProgress<float> progress = null) where TResult : class
        {
            var requestTask = new Func<Task<TResult>>(async () => await webRequest.Delete<TResult>(progress));

            var request = await Request(webRequest, requestTask);

            return request.Result;
        }

        private async Task<TResult> Request<TResult>(TWebRequest webRequest, Func<TResult> requestTask) where TResult : class
        {
            // 通信待ちキュー.
            await WaitQueueingRequest(webRequest);

            // キャンセルチェック.
            if (webRequest.IsCanceled){ return null; }

            // 通信中.

            cancellationTokenSource = new CancellationTokenSource();

            current = webRequest;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var retryCount = 0;

            TResult result = null;

            // リクエスト実行.
            while (true)
            {
                if (retryCount == 0)
                {
                    OnStart(webRequest);
                }

                result = await Task.Run(requestTask, cancellationTokenSource.Token);

                //------ 通信成功 ------

                if (result != null) { break; }
                
                //------ 通信キャンセル ------

                if (webRequest.IsCanceled) { break; }

                //------ エラー ------

                if (webRequest.Error != null)
                {
                    OnError(webRequest);
                }

                //------ リトライ回数オーバー ------

                if (RetryCount <= retryCount)
                {
                    OnRetryLimit(webRequest);
                    break;
                }

                //------ 通信失敗 ------

                // エラーハンドリングを待つ.
                var errorHandle = await WaitErrorHandling(webRequest);

                switch (errorHandle)
                {
                    case RequestErrorHandle.Retry:
                        retryCount++;
                        break;
                }

                // キャンセル時は通信終了.
                if (errorHandle == RequestErrorHandle.Cancel)
                {
                    break;
                }

                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    //------ リトライ ------

                    // リトライディレイ.
                    await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), cancellationTokenSource.Token);

                    OnRetry(webRequest);
                }
            }

            if (result != null)
            {
                // 正常終了.
                sw.Stop();

                OnComplete(webRequest, result, sw.Elapsed.TotalMilliseconds);
            }

            // 通信完了.
            current = null;

            return result;
        }

        protected TWebRequest SetupWebRequest(string url, IDictionary<string, object> urlParams)
        {
            var webRequest = new TWebRequest();

            webRequest.Initialize(PathUtility.Combine(HostUrl, url), Compress, Format);

            foreach (var header in Headers)
            {
                webRequest.Headers.Add(header.Key, header.Value);
            }

            if (urlParams != null)
            {
                foreach (var urlParam in urlParams)
                {
                    webRequest.UrlParams.Add(urlParam.Key, urlParam.Value);
                }
            }

            return webRequest;
        }

        /// <summary>
        /// 通信中の全リクエストを強制中止.
        /// </summary>
        protected void ForceCancelAll()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
            }

            if (current != null)
            {
                current.Cancel();
                current = null;
            }

            if (requestQueue.Any())
            {
                requestQueue.Clear();
            }

            OnCancel();
        }

        /// <summary>
        /// 通信処理が同時に実行されないようにキューイング.
        /// </summary>
        private async Task WaitQueueingRequest(TWebRequest webRequest)
        {
            // キューに追加.
            requestQueue.Enqueue(webRequest);

            while (true)
            {
                // キューが空になっていた場合はキャンセル扱い.
                if (requestQueue.IsEmpty())
                {
                    webRequest.Cancel(true);
                    break;
                }

                // 通信中のリクエストが存在しない & キューの先頭が自身の場合待ち終了.
                if (current == null && requestQueue.Peek() == webRequest)
                {
                    requestQueue.Dequeue();
                    break;
                }

                await Task.Delay(1);
            }
        }

        /// <summary> 開始時イベント. </summary>
        protected virtual void OnStart(TWebRequest webRequest) { }

        /// <summary> 成功時イベント. </summary>
        protected virtual void OnComplete<TResult>(TWebRequest webRequest, TResult result, double totalMilliseconds) { }

        /// <summary> 通信エラーのハンドリング. </summary>
        protected virtual Task<RequestErrorHandle> WaitErrorHandling(TWebRequest webRequest)
        {
            return Task.FromResult(RequestErrorHandle.None);
        }

        /// <summary> リトライ時イベント. </summary>
        protected virtual void OnRetry(TWebRequest webRequest) { }

        /// <summary> リトライ回数を超えた時のイベント. </summary>
        protected virtual void OnRetryLimit(TWebRequest webRequest) { }

        /// <summary> 通信エラー時イベント. </summary>
        protected virtual void OnError(TWebRequest webRequest) { }

        /// <summary> 通信キャンセル時イベント. </summary>
        protected virtual void OnCancel() { }
    }
}