
#if ENABLE_UNIWEBVIEW

using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.WebView
{
    public class UniWebViewContent : WebViewContent
    {
        #if (!UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)) || UNITY_EDITOR_OSX

        //----- params -----

        //----- field -----

        //----- property -----

        public UniWebView UniWebView { get; private set; }

        //----- method -----

        public override void Initialize()
        {
            UniWebView = UnityUtility.GetComponent<UniWebView>(gameObject);

            // RectTransform追従.

            var rectTransform = transform as RectTransform;

            if (rectTransform != null)
            {
                UniWebView.ReferenceRectTransform = rectTransform;

                // 代入した時点でのRectTransformしか参照しないので毎フレーム更新.
                Observable.EveryUpdate()
                    .Subscribe(_ => UniWebView.ReferenceRectTransform = rectTransform)
                    .AddTo(this);
            }

            // URLリンクをデバイスのデフォルトブラウザで開くかどうかの設定.
            UniWebView.SetOpenLinksInExternalBrowser(false);

            // ツールバー非表示.
            UniWebView.SetShowToolbar(false);

            // 横スクロールバー.
            UniWebView.SetHorizontalScrollBarEnabled(false);

            // 縦スクロールバー.
            UniWebView.SetVerticalScrollBarEnabled(false);

            // URLScheme追加.
            UniWebView.AddUrlScheme("webview");

            #if UNITY_ANDROID

            UniWebView.SetUseWideViewPort(false);
            UniWebView.SetBackButtonEnabled(true);

            // デバッグ有効.
            #if DEBUG

            UniWebView.SetWebContentsDebuggingEnabled(Debug.isDebugBuild);

            #endif

            #endif

            #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

            UniWebView.OnPageFinished += OnPageFinished;
            UniWebView.OnPageErrorReceived += OnPageErrorReceived;

            UniWebView.OnMessageReceived += OnReceivedMessage;
            UniWebView.OnShouldClose += OnShouldClose;

            #endif
        }

        public override UniTask Load(string url)
        {
            Loading = true;

            UniWebView.Load(url);

            return WaitForLoadFinish(url);
        }

        public override async UniTask LoadHTML(string html, string url)
        {
            UniWebView.LoadHTMLString(html, url);

            await UniTask.NextFrame();
        }

        public override void Stop()
        {
            Loading = false;

            UniWebView.Stop();
        }

        public override void Show()
        {
            UniWebView.Show();
        }

        public override void Hide()
        {
            UniWebView.Hide();
        }

        public override void Reload()
        {
            UniWebView.Reload();
        }

        protected virtual void OnPageFinished(UniWebView webView, int statusCode, string url)
        {
            UniWebView = webView;

            Loading = false;

            if (onLoadComplete != null)
            {
                onLoadComplete.OnNext(Unit.Default);
            }
        }

        protected virtual void OnPageErrorReceived(UniWebView webView, int errorCode, string errorMessage)
        {
            UniWebView = webView;
            
            Loading = false;

            if (onLoadError != null)
            {
                onLoadError.OnNext(string.Format("{0}:{1}", errorCode, errorMessage));
            }
        }

        protected virtual void OnReceivedMessage(UniWebView webView, UniWebViewMessage mes)
        {
            UniWebView = webView;

            if (onReceivedMessage != null)
            {
                onReceivedMessage.OnNext(mes.Scheme); // TODO: 仮.
            }
        }

        protected virtual bool OnShouldClose(UniWebView webView)
        {
            if (UniWebView == webView)
            {
                UniWebView = null;

                if (onClose != null)
                {
                    onClose.OnNext(Unit.Default);
                }

                return true;
            }

            return false;
        }

        public void LoadHTMLString(string baseUrl, string text)
        {
            Loading = true;

            UniWebView.LoadHTMLString(text, baseUrl);
        }

        #endif
    }
}

#endif