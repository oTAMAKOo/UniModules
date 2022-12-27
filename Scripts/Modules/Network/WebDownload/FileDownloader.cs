
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Net.WebDownload
{
    public abstract class FileDownloader<TDownloadRequest> : LifetimeDisposable where TDownloadRequest : DownloadRequest, new()
    {
        //----- params -----

        protected enum RequestErrorHandle
        {
            Retry,
            Cancel,
        }

        private sealed class DownloadInfo
        {
            public TDownloadRequest Request { get; private set; }
            public IObservable<Unit> Task { get; private set; }

            public DownloadInfo(TDownloadRequest request, IObservable<Unit> task)
            {
                Request = request;
                Task = task;
            }
        }

        //----- field -----

        private Dictionary<string, DownloadInfo> downloading = null;
        private Queue<TDownloadRequest> downloadQueueing = null;

        private bool initialized = false;

        //----- property -----

        /// <summary> 接続先URL. </summary>
        public string ServerUrl { get; private set; }

        /// <summary> 同時ダウンロード数. </summary>
        public int MaxMultiDownloadNum { get; private set; }

        /// <summary> リトライ回数. </summary>
        public int RetryCount { get; private set; }

        /// <summary> リトライするまでの時間(秒). </summary>
        public float RetryDelaySeconds { get; private set; }

        //----- method -----

        public void Initialize(int multiDownloadNum = 5, int retryCount = 3, float retryDelaySeconds = 2)
        {
            if (initialized) { return; }

            downloading = new Dictionary<string, DownloadInfo>();
            downloadQueueing = new Queue<TDownloadRequest>();

            MaxMultiDownloadNum = multiDownloadNum;
            RetryCount = retryCount;
            RetryDelaySeconds = retryDelaySeconds;

            OnInitialize();

            initialized = true;
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
        /// 指定されたURLからPostでデータを取得.
        /// </summary>
        protected IObservable<Unit> Download(TDownloadRequest downloadRequest, IProgress<float> progress = null)
        {
            // 既にダウンロードキューに入っている場合は既存のObservableを返す.
            if (downloading.ContainsKey(downloadRequest.Url))
            {
                return downloading[downloadRequest.Url].Task;
            }

            var observable = Observable.FromMicroCoroutine(() => SnedRequestInternal(downloadRequest, progress)).Share();

            downloading.Add(downloadRequest.Url, new DownloadInfo(downloadRequest, observable));

            return observable;
        }

        /// <summary> リクエスト制御 </summary>
        private IEnumerator SnedRequestInternal(TDownloadRequest downloadRequest, IProgress<float> progress)
        {
            //----------------------------------------------------------------------------------
            // ※ 呼び出し頻度が高いのでFromMicroCoroutineで呼び出される.
            //    戻り値を返す時はyield return null以外使わない.
            //----------------------------------------------------------------------------------

            // 通信待ちキュー.
            var waitQueueingYield = Observable.FromMicroCoroutine(() => WaitQueueingRequest(downloadRequest)).ToYieldInstruction(false);

            while (!waitQueueingYield.IsDone)
            {
                yield return null;
            }
            
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var retryCount = 0;

            // リクエスト実行.
            while (true)
            {
                var requestYield = downloadRequest.Download(progress).ToYieldInstruction(false);

                while (!requestYield.IsDone)
                {
                    yield return null;
                }

                //------ 通信成功 ------

                if (requestYield.HasResult && !requestYield.HasError){ break; }

                //------ 通信キャンセル ------

                if (requestYield.IsCanceled) { break; }

                //------ リトライ回数オーバー ------

                if (RetryCount <= retryCount)
                {
                    OnRetryLimit(downloadRequest);
                    break;
                }

                //------ 通信失敗 ------

                // エラーハンドリングを待つ.
                var waitErrorHandlingYield = OnError(downloadRequest, requestYield.Error).ToYieldInstruction(false);

                while (!waitErrorHandlingYield.IsDone)
                {
                    yield return null;
                }

                if (waitErrorHandlingYield.HasResult)
                {
                    switch (waitErrorHandlingYield.Result)
                    {
                        case RequestErrorHandle.Retry:
                            retryCount++;
                            break;
                    }

                    // キャンセル時は通信終了.
                    if (waitErrorHandlingYield.Result == RequestErrorHandle.Cancel)
                    {
                        break;
                    }
                }

                // リトライディレイ.
                var retryDelayYield = Observable.Timer(TimeSpan.FromSeconds(RetryDelaySeconds)).ToYieldInstruction();

                while (!retryDelayYield.IsDone)
                {
                    yield return null;
                }

            }

            sw.Stop();

            // 正常終了.
            OnComplete(downloadRequest, sw.Elapsed.TotalMilliseconds);

            // 通信完了.
            downloading.Remove(downloadRequest.Url);
        }

        /// <summary>
        /// ダウンロード中の全リクエストを強制中止.
        /// </summary>
        protected void ForceCancelAll()
        {
            if (downloading.Any())
            {
                foreach (var info in downloading.Values)
                {
                    info.Request.Cancel();
                }

                downloading.Clear();
            }

            if (downloadQueueing.Any())
            {
                downloadQueueing.Clear();
            }
        }

        /// <summary>
        /// 通信処理が同時に実行されないようにキューイング.
        /// </summary>
        private IEnumerator WaitQueueingRequest(TDownloadRequest downloadRequest)
        {
            // キューに追加.
            downloadQueueing.Enqueue(downloadRequest);

            while (true)
            {
                // キューが空になっていた場合はキャンセル扱い.
                if (downloadQueueing.IsEmpty())
                {
                    downloadRequest.Cancel(true);
                    break;
                }

                // 通信中のリクエストが存在しない & キューの先頭が自身の場合待ち終了.
                if (downloading.Count <= MaxMultiDownloadNum && downloadQueueing.Peek() == downloadRequest)
                {
                    downloadQueueing.Dequeue();
                    break;
                }

                yield return null;
            }
        }

        /// <summary> 初期化処理. </summary>
        protected virtual void OnInitialize() { }

        /// <summary> 成功時イベント. </summary>
        protected abstract void OnComplete(TDownloadRequest downloadRequest, double totalMilliseconds);
        
        /// <summary> 通信エラー時イベント. </summary>
        protected abstract IObservable<RequestErrorHandle> OnError(TDownloadRequest downloadRequest, Exception ex);

        /// <summary> リトライ回数を超えた時のイベント. </summary>
        protected abstract void OnRetryLimit(TDownloadRequest downloadRequest);
    }
}
