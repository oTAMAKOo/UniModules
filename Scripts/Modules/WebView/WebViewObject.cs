
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Modules.WebView
{
    public abstract class WebViewObject : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        protected WebViewContent webViewContent = null;

        private Subject<Unit> onLoad = null;
        private Subject<Unit> onStop = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public async UniTask Initialize()
        {
            if (initialized) { return; }

            webViewContent = await CreateContent();

            await OnInitialize();

            if (webViewContent != null)
            {
                await webViewContent.Initialize();
            }

            initialized = true;
        }

        protected virtual UniTask OnInitialize()
        {
            return UniTask.CompletedTask;
        }

        public async UniTask Load(string url)
        {
            await Initialize();

            await OnLoad(url);
            
            if (webViewContent != null)
            {
                await webViewContent.Load(url);
            }

            if (onLoad != null)
            {
                onLoad.OnNext(Unit.Default);
            }
        }

        public async UniTask LoadHTML(string html, string url = null)
        {
            await Initialize();

            await OnLoad(url);
            
            if (webViewContent != null)
            {
                await webViewContent.LoadHTML(html, url);
            }

            if (onLoad != null)
            {
                onLoad.OnNext(Unit.Default);
            }

            await UniTask.DelayFrame(30);
        }

        public virtual UniTask OnLoad(string url)
        {
            return UniTask.CompletedTask;
        }

        public void Stop()
        {
            OnStop();

            if (webViewContent != null)
            {
                webViewContent.Stop();
            }
        }

        public virtual void OnStop() { }

        public void Reload()
        {
            webViewContent.Reload();
        }

        public IObservable<Unit> OnLoadAsObservable()
        {
            return onLoad ?? (onLoad = new Subject<Unit>());
        }

        public IObservable<Unit> OnStopAsObservable()
        {
            return onStop ?? (onStop = new Subject<Unit>());
        }

        protected abstract UniTask<WebViewContent> CreateContent();
    }
}