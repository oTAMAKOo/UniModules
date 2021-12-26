
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Extensions;
using UniRx;

namespace Modules.WebView
{
    public abstract class WebViewContent : MonoBehaviour 
    {
        //----- params -----

        private const int DefaultTimeOutSeconds = 5;

        private const int DefaultRetryCount = 3;

        private const int DefaultRetryDelaySeconds = 1;

        //----- field -----

        protected Subject<Unit> onLoadComplete = null;
        protected Subject<Unit> onLoadTimeout = null;
        protected Subject<string> onLoadError = null;
        protected Subject<string> onReceivedMessage = null; // TODO: 仮.
        protected Subject<Unit> onClose = null;

        //----- property -----

        public bool Loading { get; protected set; } = false;

        public int TimeOutSeconds { get; set; } = DefaultTimeOutSeconds;

        public int RetryCount { get; set; } = DefaultRetryCount;

        public int RetryDelaySeconds { get; set; } = DefaultRetryDelaySeconds;

        //----- method -----

        public virtual void Initialize() { }

        public virtual UniTask Load(string url)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask LoadHTML(string html, string url)
        {
            return UniTask.CompletedTask;
        }

        public virtual void Stop() { }

        public virtual void Show() { }

        public virtual void Hide() { }

        public virtual void Reload(){ }

        protected UniTask WaitForLoadFinish(string url)
        {
            Action<TimeoutException> onTimeout = ex =>
            {
                Debug.LogErrorFormat("[WebView Timeout] {0}\n\n{1}", url, ex);

                if (onLoadTimeout != null)
                {
                    onLoadTimeout.OnNext(Unit.Default);
                }
            };

            Action<Exception> onError = ex =>
            {
                Debug.LogErrorFormat("[WebView Error] {0}\n\n{1}", url, ex);

                Stop();

                if (onLoadError != null)
                {
                    onLoadError.OnNext(ex.Message);
                }
            };

            return Observable.EveryUpdate()
                .Timeout(new TimeSpan(0, 0, 0, TimeOutSeconds))
                .OnErrorRetry((TimeoutException ex) => onTimeout(ex), RetryCount, new TimeSpan(0, 0, 0, RetryDelaySeconds))
                .DoOnError(x => onError.Invoke(x))
                .SkipWhile(_ => Loading)
                .First()
                .ToUniTask();
        }

        public IObservable<Unit> OnLoadCompleteAsObservable()
        {
            return onLoadComplete ?? (onLoadComplete = new Subject<Unit>());
        }

        public IObservable<string> OnLoadErrorAsObservable()
        {
            return onLoadError ?? (onLoadError = new Subject<string>());
        }

        public IObservable<Unit> OnLoadTimeOutAsObservable()
        {
            return onLoadTimeout ?? (onLoadTimeout = new Subject<Unit>());
        }

        // TODO: 仮.
        public IObservable<string> OnReceivedMessageAsObservable()
        {
            return onReceivedMessage ?? (onReceivedMessage = new Subject<string>());
        }

        public IObservable<Unit> OnCloseAsObservable()
        {
            return onClose ?? (onClose = new Subject<Unit>());
        }
    }
}