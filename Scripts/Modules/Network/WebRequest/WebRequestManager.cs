
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensions;

namespace Modules.Network.WebRequest
{
    public enum Method
    {
        None,

        [Label("POST")]
        Post,
        [Label("PUT")]
        Put,
        [Label("PATCH")]
        Patch,
        [Label("GET")]
        Get,
        [Label("DELETE")]
        Delete,
    }

    public enum DataFormat
    {
        Json,
        MessagePack,
    }

    public enum DataCompressType
    {
        None = 0,

        GZip,
        Deflate,
        MessagePackLZ4,
    }

    public abstract class WebRequestManager<TInstance, TWebRequest> : Singleton<TInstance>
        where TInstance : WebRequestManager<TInstance, TWebRequest> 
        where TWebRequest : class, IWebRequestClient, IDisposable, new()
    {
        //----- params -----

        /// <summary> デフォルトリトライ回数 </summary>
        private const int DefaultRetryCount = 3;
        /// <summary> デフォルトリトライ待ち秒数 </summary>
        private const int DefaultRetryDelaySeconds = 2;

        /// <summary> HTTP 408 Request Timeout. </summary>
        private const long RequestTimeoutStatusCode = 408;
        /// <summary> HTTP 429 Too Many Requests. </summary>
        private const long TooManyRequestsStatusCode = 429;

        protected enum RequestErrorHandle
        {
            None = 0,

            Retry,
            Cancel,
            Exit,
        }

        //----- field -----

        private List<TWebRequest> requestList = null;

        private Queue<TWebRequest> requestQueue = null;
        
        private CancellationTokenSource cancellationTokenSource = null;

        //----- property -----

        /// <summary> 接続先URL. </summary>
        public string HostUrl { get; private set; }

        /// <summary> 送信データの圧縮. </summary>
        public DataCompressType CompressRequestData { get; private set; } 

        /// <summary> 受信データの圧縮. </summary>
        public DataCompressType CompressResponseData { get; private set; }

        /// <summary> データ内容フォーマット. </summary>
        public DataFormat Format { get; private set; }

        /// <summary> リトライ回数. </summary>
        public int RetryCount { get; private set; }

        /// <summary> リトライするまでの時間(秒). </summary>
        public float RetryDelaySeconds { get; private set; }

        /// <summary> ヘッダー情報 [key, (encrypted, value)]. </summary>
        public IDictionary<string, Tuple<bool, string>> Headers { get; private set; }

        //----- method -----

        protected WebRequestManager()
        {
            requestList = new List<TWebRequest>();
            requestQueue = new Queue<TWebRequest>();
            Headers = new Dictionary<string, Tuple<bool, string>>();

            CompressRequestData = DataCompressType.GZip;
            CompressResponseData = DataCompressType.GZip;
            Format = DataFormat.MessagePack;

            RetryCount = DefaultRetryCount;
            RetryDelaySeconds = DefaultRetryDelaySeconds;
        }

        public virtual void SetHostUrl(string hostUrl)
        {
            HostUrl = hostUrl;
        }

        public virtual void SetFormat(DataFormat format)
        {
            Format = format;
        }

        public virtual void SetRetryCount(int retryCount)
        {
            RetryCount = retryCount;
        }

        public virtual void SetRetryDelaySeconds(int retryDelaySeconds)
        {
            RetryDelaySeconds = retryDelaySeconds;
        }

        public void SetRequestDataCompress(DataCompressType compressType)
        {
            CompressRequestData = compressType;
        }

        public void SetResponseDataCompress(DataCompressType compressType)
        {
            CompressResponseData = compressType;
        }

        /// <summary> リソースの取得. </summary>
        /// <param name="retry"> 失敗時のリトライを許可するか. null の場合は <see cref="IsRetryable"/> に従う. 許可しても <see cref="IsRetryableError"/> が false のエラーではリトライしない. </param>
        protected async Task<TResult> Get<TResult>(TWebRequest webRequest, bool parallel = false, IProgress<float> progress = null, bool? retry = null) where TResult : class
        {
            using (webRequest)
            {
                webRequest.SetMethod(Method.Get);

                var taskFunc = webRequest.Get<TResult>(progress);

                var result = await Request(webRequest, taskFunc, parallel, retry);

                return result;
            }
        }

        /// <summary> リソースの作成、追加. </summary>
        /// <param name="retry"> 失敗時のリトライを許可するか. null の場合は <see cref="IsRetryable"/> に従う. 許可しても <see cref="IsRetryableError"/> が false のエラーではリトライしない. </param>
        protected async Task<TResult> Post<TResult, TContent>(TWebRequest webRequest, TContent content, bool parallel = false, IProgress<float> progress = null, bool? retry = null) where TResult : class
        {
            using (webRequest)
            {
                webRequest.SetMethod(Method.Post);

                var taskFunc = webRequest.Post<TResult, TContent>(content, progress);

                var result = await Request(webRequest, taskFunc, parallel, retry);

                return result;
            }
        }

        /// <summary> リソースの更新、作成. </summary>
        /// <param name="retry"> 失敗時のリトライを許可するか. null の場合は <see cref="IsRetryable"/> に従う. 許可しても <see cref="IsRetryableError"/> が false のエラーではリトライしない. </param>
        protected async Task<TResult> Put<TResult, TContent>(TWebRequest webRequest, TContent content, bool parallel = false, IProgress<float> progress = null, bool? retry = null) where TResult : class
        {
            using (webRequest)
            {
                webRequest.SetMethod(Method.Put);

                var taskFunc = webRequest.Put<TResult, TContent>(content, progress);

                var result = await Request(webRequest, taskFunc, parallel, retry);

                return result;
            }
        }

        /// <summary> リソースの部分更新. </summary>
        /// <param name="retry"> 失敗時のリトライを許可するか. null の場合は <see cref="IsRetryable"/> に従う. 許可しても <see cref="IsRetryableError"/> が false のエラーではリトライしない. </param>
        protected async Task<TResult> Patch<TResult, TContent>(TWebRequest webRequest, TContent content, bool parallel = false, IProgress<float> progress = null, bool? retry = null) where TResult : class
        {
            using (webRequest)
            {
                webRequest.SetMethod(Method.Patch);

                var taskFunc = webRequest.Patch<TResult, TContent>(content, progress);

                var result = await Request(webRequest, taskFunc, parallel, retry);

                return result;
            }
        }

        /// <summary> リソースの削除. </summary>
        /// <param name="retry"> 失敗時のリトライを許可するか. null の場合は <see cref="IsRetryable"/> に従う. 許可しても <see cref="IsRetryableError"/> が false のエラーではリトライしない. </param>
        protected async Task<TResult> Delete<TResult>(TWebRequest webRequest, bool parallel = false, IProgress<float> progress = null, bool? retry = null) where TResult : class
        {
            using (webRequest)
            {
                webRequest.SetMethod(Method.Delete);

                var taskFunc = webRequest.Delete<TResult>(progress);

                var result = await Request(webRequest, taskFunc, parallel, retry);

                return result;
            }
        }

        private async Task<TResult> Request<TResult>(TWebRequest webRequest, Func<CancellationToken, Task<TResult>> taskFunc, bool parallel, bool? retry) where TResult : class
        {
			TResult result = null;

			if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
			{
				cancellationTokenSource = new CancellationTokenSource();
			}

			// CancelAll で cancellationTokenSource が破棄されても、開始時のトークンで中止を判定する.
			var cancelToken = cancellationTokenSource.Token;

			try
			{
				// 実行待ち.

				if (!parallel)
				{
					await WaitQueueingRequest(webRequest, cancelToken);
				}

				if (webRequest.IsCanceled) { return null; }

				// 通信中.

				requestList.Add(webRequest);

				var sw = System.Diagnostics.Stopwatch.StartNew();

				var retryCount = 0;

				var success = false;

				var retryable = retry.HasValue ? retry.Value : IsRetryable(webRequest);

				// リクエスト実行.
				while (true)
				{
					if (retryCount == 0)
					{
						OnStart(webRequest);
					}

					result = await taskFunc.Invoke(cancelToken);

                    //------ 通信成功 ------

                    if (result != null)
                    {
                        success = true;
                        break;
                    }

                    // 受信データが空の成功応答.
                    if (webRequest.IsSuccess && IsEmptyResponseSuccess(webRequest))
                    {
                        success = true;
                        break;
                    }

                    //------ 通信キャンセル ------

                    if (webRequest.IsCanceled || cancelToken.IsCancellationRequested) { break; }

                    //------ リトライしない / リトライ回数オーバー ------

                    var stopRetry = !retryable || !IsRetryableError(webRequest);

                    if (stopRetry || RetryCount <= retryCount)
                    {
                        // リトライしない場合は、発生したエラーを通知してから終了する.
                        if (stopRetry && webRequest.Error != null)
                        {
                            OnError(webRequest);
                        }

                        OnRetryLimit(webRequest);
                        break;
                    }

                    //------ エラー ------

                    if (webRequest.Error != null)
                    {
                        OnError(webRequest);
                    }

                    //------ 通信失敗 ------

                    var exit = false;

                    // エラーハンドリングを待つ.

                    var errorHandle = await WaitErrorHandling(webRequest);

                    switch (errorHandle)
                    {
                        case RequestErrorHandle.Retry:
                            retryCount++;
                            break;

                        case RequestErrorHandle.Cancel:
                        case RequestErrorHandle.Exit:
                            exit = true;
                            break;
                    }

                    // 通信終了.
                    if (exit)
                    {
                        break;
                    }

                    //------ 通信キャンセル ------

                    if (webRequest.IsCanceled) { break; }

                    // キャンセル時は通信終了.
                    if (errorHandle == RequestErrorHandle.Cancel)
                    {
                        break;
                    }

                    //------ リトライ ------

                    if (!cancelToken.IsCancellationRequested)
                    {
                        // リトライディレイ.
                        await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), cancelToken);

                        OnRetry(webRequest);
                    }
				}

				if (success)
				{
					// 正常終了.
					sw.Stop();

					OnComplete(webRequest, result, sw.Elapsed.TotalMilliseconds);
				}
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			finally
			{
				requestList.Remove(webRequest);
			}
			
            return result;
        }

        protected TWebRequest SetupWebRequest(string url, IDictionary<string, object> urlParams)
        {
            var webRequest = new TWebRequest();

            webRequest.Initialize(PathUtility.Combine(HostUrl, url), Format);

            webRequest.SetRequestDataCompress(CompressRequestData);
            webRequest.SetResponseDataCompress(CompressResponseData);

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

        /// <summary> 通信中の全リクエストを中止. </summary>
        public void CancelAll()
        {
            if (cancellationTokenSource != null)
            {
				cancellationTokenSource.Cancel();
				cancellationTokenSource = null;
            }

			foreach (var request in requestList)
			{
				if (request == null){ continue; }

				request.Cancel();
			}

			requestList.Clear();

			if (requestQueue.Any())
            {
                requestQueue.Clear();
            }

            OnCancel();
        }

        /// <summary> 通信が同時に実行されないようにキューイング. </summary>
        private async Task WaitQueueingRequest(TWebRequest webRequest, CancellationToken cancelToken)
        {
            // キューに追加.
            requestQueue.Enqueue(webRequest);

            while (true)
            {
				// キューが空になっていた場合はキャンセル扱い.
                if (requestQueue.IsEmpty())
                {
                    webRequest.Cancel();
                    break;
                }

                // 通信中のリクエストが存在しない & キューの先頭が自身の場合待ち終了.
                if (requestList.IsEmpty() && requestQueue.Peek() == webRequest)
                {
                    requestQueue.Dequeue();
                    break;
                }

				// キャンセルされたリクエストは後続の待ちを止めないようにキューから外す.
				if (webRequest.IsCanceled)
				{
					requestQueue = new Queue<TWebRequest>(requestQueue.Where(x => x != webRequest));
					break;
				}

				await Task.Delay(1, cancelToken);
            }
        }

        /// <summary> 通信エラーのハンドリング. </summary>
        protected virtual Task<RequestErrorHandle> WaitErrorHandling(TWebRequest webRequest)
        {
            if (webRequest.Error is NetworkReachabilityException)
            {
                return Task.FromResult(RequestErrorHandle.Exit);
            }

            return Task.FromResult(RequestErrorHandle.Retry);
        }

        /// <summary> 呼び出し側でリトライの可否を指定しなかった時に、リトライしてよいリクエストか. </summary>
        protected virtual bool IsRetryable(TWebRequest webRequest)
        {
            return true;
        }

        /// <summary> 発生したエラーがリトライしてよいものか. リトライを許可したリクエストでも false ならリトライしない. </summary>
        protected virtual bool IsRetryableError(TWebRequest webRequest)
        {
            return true;
        }

        /// <summary> 受信データが空 (デシリアライズ結果が null) の成功応答を成功として扱うか. </summary>
        protected virtual bool IsEmptyResponseSuccess(TWebRequest webRequest)
        {
            return false;
        }

        /// <summary> 再送しても結果が変わらないクライアントエラー (4xx. 408・429 を除く) か. </summary>
        protected static bool IsNonRetryableClientError(TWebRequest webRequest)
        {
            var responseCode = webRequest.ResponseCode;

            if (responseCode == RequestTimeoutStatusCode || responseCode == TooManyRequestsStatusCode) { return false; }

            return 400 <= responseCode && responseCode < 500;
        }

        /// <summary> 開始時イベント. </summary>
        protected virtual void OnStart(TWebRequest webRequest) { }

        /// <summary> 成功時イベント. </summary>
        protected virtual void OnComplete<TResult>(TWebRequest webRequest, TResult result, double totalMilliseconds) { }

        /// <summary> リトライ時イベント. </summary>
        protected virtual void OnRetry(TWebRequest webRequest) { }

        /// <summary> リトライ回数を超えた時、またはリトライしないと判定した時のイベント. </summary>
        protected virtual void OnRetryLimit(TWebRequest webRequest) { }

        /// <summary> 通信エラー時イベント. </summary>
        protected virtual void OnError(TWebRequest webRequest) { }

        /// <summary> 通信キャンセル時イベント. </summary>
        protected virtual void OnCancel() { }
    }
}