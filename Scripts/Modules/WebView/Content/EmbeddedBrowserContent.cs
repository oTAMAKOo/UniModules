
#if ENABLE_EMBEDDEDBROWSER

using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ZenFulcrum.EmbeddedBrowser;
using UniRx;
using UniRx.Triggers;
using Extensions;

namespace Modules.WebView
{
    public class EmbeddedBrowserContent : WebViewContent
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

        //----- params -----

        //----- field -----

        //----- property -----

        public Browser EmbeddedBrowser { get; private set; }

        //----- method -----

        public override void Initialize()
        {
            EmbeddedBrowser = UnityUtility.GetComponent<Browser>(gameObject);

            EmbeddedBrowser.RegisterFunction("openurl", OpenUrlCallback);

            EmbeddedBrowser.onConsoleMessage += (message, source) =>
            {
                Debug.LogFormat("{0}, {2}", message, source);
            };

            EmbeddedBrowser.OnDestroyAsObservable()
                .Subscribe(_ =>
                    {
                        CloseWebView();
                    })
                .AddTo(this);

            Loading = false;

            EmbeddedBrowser.onLoad += OnLoadCallbak;
        }

        public override async UniTask Load(string url)
        {
            Loading = true;

            EmbeddedBrowser.Url = url;

            // ※ EmbeddedBrowserは読み込み待ちできない.
            // 読み込み時間が気になる場合はLoadHTMLで処理を行う.
            Loading = false;

            await WaitForLoadFinish(url);
        }

        public override async UniTask LoadHTML(string html, string url)
        {
            EmbeddedBrowser.LoadHTML(html, url);

            await UniTask.NextFrame();
        }

        public override void Stop()
        {
            Loading = false;

            EmbeddedBrowser.Stop();
        }

        public override void Show()
        {
            UnityUtility.SetActive(EmbeddedBrowser, true);
        }

        public override void Hide()
        {
            UnityUtility.SetActive(EmbeddedBrowser, false);
        }

        public override void Reload()
        {
            EmbeddedBrowser.Reload();
        }

        protected virtual void OnLoadCallbak(JSONNode data)
        {
            var nodeDictionary = data.Value as IDictionary<string, JSONNode>;

            if (nodeDictionary != null)
            {
                var node = nodeDictionary.GetValueOrDefault("url");

                if (node != null)
                {
                    var nodeString = node.Value as string;

                    if (nodeString == "webview://back")
                    {
                        CloseWebView();
                    }
                }
            }
        }

        private void CloseWebView()
        {
            Loading = false;

            UnityUtility.SafeDelete(EmbeddedBrowser);

            EmbeddedBrowser = null;
            
            if (onClose != null)
            {
                onClose.OnNext(Unit.Default);
            }
        }

        protected virtual void OpenUrlCallback(JSONNode args)
        {
            var linkUrl = args[0].Value as string;
                    
            if (!string.IsNullOrEmpty(linkUrl))
            {
                Application.OpenURL(linkUrl);
            }
        }

        #endif
    }
}

#endif