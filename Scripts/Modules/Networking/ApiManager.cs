﻿
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Networking
{
    public abstract class ApiManager<TInstance, TWebRequest> : Singleton<TInstance>
        where TInstance : ApiManager<TInstance, TWebRequest> where TWebRequest : WebRequest, new()
    {
        //----- params -----

        //----- field -----

        private string serverUrl = null;
        private TWebRequest currentWebRequest = null;
        private Queue<TWebRequest> webRequestQueue = null;

        //----- property -----

        public string ServerUrl { get { return serverUrl; } }

        public DataFormat Format { get; set; }

        public IDictionary<string, string> Headers { get; private set; }

        // タイムアウトするまでの時間(秒).
        protected abstract float TimeOutSeconds { get; }

        // リトライ回数.
        protected abstract int RetryCount { get; }

        // リトライするまでの時間(秒).
        protected abstract float RetryDelaySeconds { get; }

        //----- method -----

        protected ApiManager()
        {
            webRequestQueue = new Queue<TWebRequest>();
            Headers = new Dictionary<string, string>();
        }

        public virtual void Initialize(string serverUrl, DataFormat format = DataFormat.MessagePack)
        {
            this.serverUrl = serverUrl;

            Format = format;
        }

        /// <summary>
        /// 指定されたURLからGetでデータを取得.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="webRequest"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        protected IObservable<TResult> Get<TResult>(TWebRequest webRequest, UniRx.IProgress<float> progress = null) where TResult : class
        {
            var requestObserver = Observable.Defer(() => webRequest.Get<TResult>(progress));

            return Observable.FromCoroutine(() => WaitWebRequestStart(webRequest))
                .SelectMany(_ => FetchCore(requestObserver, webRequest));
        }

        /// <summary>
        /// 指定されたURLからPostでデータを取得.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TContent"></typeparam>
        /// <param name="webRequest"></param>
        /// <param name="content"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        protected IObservable<TResult> Post<TResult, TContent>(TWebRequest webRequest, TContent content, UniRx.IProgress<float> progress = null) where TResult : class
        {
            var requestObserver = Observable.Defer(() => webRequest.Post<TResult, TContent>(content, progress));

            return Observable.FromCoroutine(() => WaitWebRequestStart(webRequest))
                .SelectMany(_ => FetchCore(requestObserver, webRequest));
        }

        /// <summary>
        /// 通信中の全リクエストを強制中止.
        /// </summary>
        protected void ForceCancelAll()
        {
            if (currentWebRequest != null)
            {
                currentWebRequest.Cancel();
                currentWebRequest = null;
            }

            if (webRequestQueue.Any())
            {
                webRequestQueue.Clear();
            }
        }

        /// <summary>
        /// 通信処理が同時に実行されないようにキューイング.
        /// </summary>
        private IEnumerator WaitWebRequestStart(TWebRequest webRequest)
        {
            // キューに追加.
            webRequestQueue.Enqueue(webRequest);

            while (true)
            {
                // キューが空になっていた場合はキャンセル扱い.
                if (webRequestQueue.IsEmpty())
                {
                    webRequest.Cancel(true);
                    yield break;
                }

                // 通信中のリクエストが存在しない & キューの先頭が自身の場合待ち終了.
                if (currentWebRequest == null && webRequestQueue.Peek() == webRequest)
                {
                    webRequestQueue.Dequeue();
                    yield break;
                }

                yield return null;
            }
        }

        private IObservable<TResult> FetchCore<TResult>(IObservable<TResult> observer, TWebRequest webRequest)
        {
            // 通信中.
            currentWebRequest = webRequest;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            return observer
                // タイムアウト時間設定.
                .Timeout(TimeSpan.FromSeconds(TimeOutSeconds))
                // リトライ処理.
                .OnErrorRetry((TimeoutException ex) => OnTimeout(webRequest, ex), RetryCount, TimeSpan.FromSeconds(RetryDelaySeconds))
                // エラー処理.
                .DoOnError((Exception ex) =>
                    {
                        OnError(webRequest, ex);
                        ForceCancelAll();
                    })
                // 正常終処理.
                .Do(x =>
                    {
                        if (x != null)
                        {
                            sw.Stop();
                            OnComplete(webRequest, x, sw.Elapsed.TotalMilliseconds);
                        }
                    })
                // 通信完了.
                .Finally(() => currentWebRequest = null);
        }

        protected TWebRequest SetupWebRequest(string url, IDictionary<string, object> urlParams)
        {
            var webRequest = new TWebRequest();

            webRequest.Initialize(PathUtility.Combine(serverUrl, url), Format);

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

        protected abstract void OnTimeout(TWebRequest webRequest, Exception ex);

        protected abstract void OnError(TWebRequest webRequest, Exception ex);

        protected abstract void OnComplete<TResult>(TWebRequest webRequest, TResult result, double totalMilliseconds);
    }
}