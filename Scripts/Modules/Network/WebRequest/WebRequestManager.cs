
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

        protected enum RequestErrorHandle
        {
            None = 0,

            Retry,
            Cancel,
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

        /// <summary> ヘッダー情報 [key, (encrypted, value)]. </summary>
        public IDictionary<string, Tuple<bool, string>> Headers { get; private set; }

        /// <summary> リトライ回数. </summary>
        public int RetryCount { get; private set; }

        /// <summary> リトライするまでの時間(秒). </summary>
        public float RetryDelaySeconds { get; private set; }

        //----- method -----

        protected WebRequestManager()
        {
            requestList = new List<TWebRequest>();
            requestQueue = new Queue<TWebRequest>();
            Headers = new Dictionary<string, Tuple<bool, string>>();
        }

        public virtual void Initialize(string hostUrl, DataFormat format = DataFormat.MessagePack, int retryCount = 3, float retryDelaySeconds = 2)
        {
            HostUrl = hostUrl;
            Format = format;
            RetryCount = retryCount;
            RetryDelaySeconds = retryDelaySeconds;
        }

        public void SetFormat(DataFormat format)
        {
            Format = format;
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
        protected async Task<TResult> Get<TResult>(TWebRequest webRequest, bool parallel = false, IProgress<float> progress = null) where TResult : class
        {
            using (webRequest)
            {
                var taskFunc = webRequest.Get<TResult>(progress);

                var result = await Request(webRequest, taskFunc, parallel);

                return result;
            }
        }

        /// <summary> リソースの作成、追加. </summary>
        protected async Task<TResult> Post<TResult, TContent>(TWebRequest webRequest, TContent content, bool parallel = false, IProgress<float> progress = null) where TResult : class
        {
            using (webRequest)
            {
                var taskFunc = webRequest.Post<TResult, TContent>(content, progress);

                var result = await Request(webRequest, taskFunc, parallel);

                return result;
            }
        }

        /// <summary> リソースの更新、作成. </summary>
        protected async Task<TResult> Put<TResult, TContent>(TWebRequest webRequest, TContent content, bool parallel = false, IProgress<float> progress = null) where TResult : class
        {
            using (webRequest)
            {
                var taskFunc = webRequest.Put<TResult, TContent>(content, progress);

                var result = await Request(webRequest, taskFunc, parallel);

                return result;
            }
        }

        /// <summary> リソースの部分更新. </summary>
        protected async Task<TResult> Patch<TResult, TContent>(TWebRequest webRequest, TContent content, bool parallel = false, IProgress<float> progress = null) where TResult : class
        {
            using (webRequest)
            {
                var taskFunc = webRequest.Patch<TResult, TContent>(content, progress);

                var result = await Request(webRequest, taskFunc, parallel);

                return result;
            }
        }

        /// <summary> リソースの削除. </summary>
        protected async Task<TResult> Delete<TResult>(TWebRequest webRequest, bool parallel = false, IProgress<float> progress = null) where TResult : class
        {
            using (webRequest)
            {
                var taskFunc = webRequest.Delete<TResult>(progress);

                var result = await Request(webRequest, taskFunc, parallel);

                return result;
            }
        }

        private async Task<TResult> Request<TResult>(TWebRequest webRequest, Func<CancellationToken, Task<TResult>> taskFunc, bool parallel) where TResult : class
        {
			TResult result = null;

			if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
			{
				cancellationTokenSource = new CancellationTokenSource();
			}

			try
			{
				// 実行待ち.

				if (!parallel)
				{
					await WaitQueueingRequest(webRequest, cancellationTokenSource.Token);
				}

				// ネットワーク接続待ち.

				await WaitNetworkReachable(cancellationTokenSource.Token);
				
				if (webRequest.IsCanceled) { return null; }

				// 通信中.

				requestList.Add(webRequest);

				var sw = System.Diagnostics.Stopwatch.StartNew();

				var retryCount = 0;

				// リクエスト実行.
				while (true)
				{
					if (retryCount == 0)
					{
						OnStart(webRequest);
					}

					result = await taskFunc.Invoke(cancellationTokenSource.Token);

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
                    webRequest.Cancel(true);
                    break;
                }

                // 通信中のリクエストが存在しない & キューの先頭が自身の場合待ち終了.
                if (requestList.IsEmpty() && requestQueue.Peek() == webRequest)
                {
                    requestQueue.Dequeue();
                    break;
                }

				if (webRequest.IsCanceled) { break; }

				await Task.Delay(1, cancelToken);
            }
        }

		/// <summary> ネットワーク接続待ち </summary>
		protected virtual Task WaitNetworkReachable(CancellationToken cancelToken) { return Task.CompletedTask; }

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