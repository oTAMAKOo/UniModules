
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

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

        private void CreateWebViewContent(GameObject prefab)
        {
            if (webViewContent != null){ return; }

            // Awakeを実行させる為一旦ルート階層に生成.
            webViewContent = UnityUtility.Instantiate<WebViewContent>(null, prefab);

            if (webViewContent != null)
            {
                webViewContent.Initialize();

                UnityUtility.SetParent(webViewContent.gameObject, gameObject);
            }
        }

        public async UniTask Initialize()
        {
            if (initialized) { return; }

            var prefab = GetContentPrefab();

            CreateWebViewContent(prefab);

            await OnInitialize();

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

            await UniTask.NextFrame();
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

        public void Show()
        {
	        webViewContent.Show();
        }

        public void Hide()
        {
	        webViewContent.Hide();
        }

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

        protected abstract GameObject GetContentPrefab();
    }
}