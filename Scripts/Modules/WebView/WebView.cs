

#if ENABLE_UNIWEBVIEW2

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using System.Collections;
using UnityEngine.Networking;

namespace Modules.WebView
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class WebView : MonoBehaviour
    {
        //----- params -----

        private const int TimeOutSeconds = 5;

        public class PostData
        {
            public string fieldName = string.Empty;
            public string value = string.Empty;
        }

        public class ShowParam
        {
            public bool fade = false;
            public UniWebViewTransitionEdge direction = UniWebViewTransitionEdge.None;
            public float duration = 0.4f;
            public Action finishAction = null;
            public bool showSpinner = false;
            public bool toolBarShowAnim = false;
            public Vector2 buffer = new Vector2(50f, 50f);
        }

        //----- field -----

        #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

        private UniWebView uniWebView = null;
        private bool loading = false;

        #endif

        private Subject<Unit> onLoadComplete = null;
        private Subject<string> onLoadError = null;
        private Subject<Unit> onClose = null;
        private Subject<UniWebViewMessage> onReceivedMessage = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public IObservable<Unit> Initialize()
        {
            if (initialized) { return Observable.ReturnUnit(); }

            #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

            uniWebView = UnityUtility.GetOrAddComponent<UniWebView>(gameObject);

            // .
            uniWebView.HideToolBar(false);

            // URLリンクをデバイスのデフォルトブラウザで開くかどうかの設定.
            uniWebView.openLinksInExternalBrowser = false;

            // 横スクロールバー.
            uniWebView.SetHorizontalScrollBarShow(false);

            // 縦スクロールバー.
            uniWebView.SetVerticalScrollBarShow(false);
            uniWebView.CleanCache();

            #endif

            #if UNITY_ANDROID

            uniWebView.SetUseWideViewPort(false);
            uniWebView.backButtonEnable = true;

            #endif

            #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

            uniWebView.OnLoadComplete += OnLoadComplete;
            uniWebView.OnReceivedMessage += OnReceivedMessage;
            uniWebView.OnWebViewShouldClose += OnWebViewShouldClose;

            #endif

            initialized = true;

            return Observable.ReturnUnit();
        }

        /// <summary>
        /// 読み込み.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public IObservable<Unit> Load(string url)
        {
            #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

            loading = true;

            uniWebView.Load(url);

            return Observable.EveryUpdate()
                .Timeout(new TimeSpan(0, 0, 0, TimeOutSeconds))
                .SkipWhile(_ => loading)
                .AsUnitObservable();

            #else

            return Observable.ReturnUnit();

            #endif
        }


        public IEnumerator GetHTML(string url, PostData[] postDataList)
        {
            if (!postDataList.Any()) { yield break; }

            var form = new List<IMultipartFormSection>();

            foreach (var postData in postDataList)
            {
                form.Add(new MultipartFormDataSection(postData.fieldName, postData.value));
            }

            var webRequest = UnityWebRequest.Post(url, form);

            var yieldResponse = Observable.FromCoroutine(_ => ResponseConnect(webRequest, 3.0f)).ToYieldInstruction();

            yield return yieldResponse;

            if (!string.IsNullOrEmpty(webRequest.error))
            {
                // リクエストエラー
                yield break;
            }

            if (string.IsNullOrEmpty(webRequest.downloadHandler.text))
            {
                // 空で返ってきた場合
                yield break;
            }

            yield return LoadHTMLString(webRequest.downloadHandler.text, url).Subscribe().AddTo(this);
        }

        private IEnumerator<UnityWebRequest> ResponseConnect(UnityWebRequest webRequest, float timeout)
        {
            var requestTime = Time.time;

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                if (Time.time - requestTime < timeout)
                {
                    yield return null;
                }
                else
                {
                    // エラー
                    break;
                }
            }

            yield return webRequest;
        }


        /// <summary>
        /// 読み込み.(Post)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private IObservable<Unit> LoadHTMLString(string text, string url)
        {
            #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

            loading = true;

            uniWebView.LoadHTMLString(text, url);

            return Observable.EveryUpdate()
                .Timeout(new TimeSpan(0, 0, 0, TimeOutSeconds))
                .SkipWhile(_ => loading)
                .AsUnitObservable();

            #else

            return Observable.ReturnUnit();

            #endif
        }

        /// <summary>
        /// 読み込み中止
        /// </summary>
        public void Stop()
        {
            #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

            if (uniWebView != null)
            {
                uniWebView.Stop();
            }

            #endif
        }

        /// <summary>
        /// WebView表示.
        /// </summary>
        public void Show(ShowParam param = null)
        {
            param = param ?? new ShowParam();

            var area = GetDrawArea(param.buffer);

            if (area != null)
            {
                #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

                uniWebView.insets = area;

                uniWebView.SetShowSpinnerWhenLoading(param.showSpinner);
                uniWebView.HideToolBar(param.toolBarShowAnim);
                uniWebView.Show(param.fade, param.direction, param.duration, param.finishAction);

                #endif
            }
        }

        /// <summary>
        /// WebViewの表示範囲設定.
        /// </summary>
        private UniWebViewEdgeInsets GetDrawArea(Vector2 buffer)
        {
            var rectTransform = UnityUtility.GetComponent<RectTransform>(gameObject);

            // 描画カメラ検索.
            var camera = UnityUtility.FindCameraForLayer(gameObject.layer).FirstOrDefault();

            if (camera != null)
            {
                // webView表示する四隅のワールド座標を取得.
                var rectCorner = new Vector3[4];
                rectTransform.GetWorldCorners(rectCorner);

                // 四隅のワールド座標をスクリーン座標に変換.
                var screenToCorner = rectCorner.Select(vec => camera.WorldToScreenPoint(vec)).ToList();

                var top = (Screen.height - screenToCorner.Max(vec => vec.y) + buffer.y + rectTransform.position.y);
                var bottom = (screenToCorner.Min(vec => vec.y) + buffer.y + rectTransform.position.y);
                var left = (screenToCorner.Min(vec => vec.x) + buffer.x + rectTransform.position.x);
                var right = (Screen.width - screenToCorner.Max(vec => vec.x) + buffer.x + rectTransform.position.x);


                #if UNITY_IOS && !UNITY_EDITOR

				// 単位ポイントあたりのスクリーンサイズを取得.
				var hightPerPoint = (float)Screen.height / (float)UniWebViewHelper.screenHeight;
				var widthPerPoint = (float)Screen.width / (float)UniWebViewHelper.screenWidth;

				// ポイントに変換.
				top = top / hightPerPoint;
                bottom = bottom / hightPerPoint;
                left = left / widthPerPoint;
                right = right / widthPerPoint;
								
                #endif

                return new UniWebViewEdgeInsets((int)top, (int)left, (int)bottom, (int)right);
            }

            Debug.LogError("WebView camera not found.");

            return null;
        }

        #if UNITY_IOS || UNITY_ANDROID || UNITY_WP8

        /// <summary>
        /// ロード終了時.
        /// </summary>
        private void OnLoadComplete(UniWebView view, bool success, string errorMessage)
        {
            if (success)
            {
                uniWebView = view;

                if (onLoadComplete != null)
                {
                    onLoadComplete.OnNext(Unit.Default);
                }
            }
            else
            {
                if (onLoadError != null)
                {
                    onLoadError.OnNext(errorMessage);
                }
            }

            loading = false;
        }

        /// <summary>
        /// メッセージを取得した際.
        /// </summary>
        private void OnReceivedMessage(UniWebView view, UniWebViewMessage mes)
        {
            if (onReceivedMessage != null)
            {
                onReceivedMessage.OnNext(mes);
            }
        }

        /// <summary>
        /// ネイティブによって閉じられた際.
        /// </summary>
        private bool OnWebViewShouldClose(UniWebView view)
        {
            if (view == uniWebView)
            {
                uniWebView = null;

                if (onClose != null)
                {
                    onClose.OnNext(Unit.Default);
                }

                return true;
            }

            return false;
        }

        #endif

        public IObservable<Unit> OnLoadCompleteAsObservable()
        {
            return onLoadComplete ?? (onLoadComplete = new Subject<Unit>());
        }

        public IObservable<string> OnLoadErrorAsObservable()
        {
            return onLoadError ?? (onLoadError = new Subject<string>());
        }

        public IObservable<UniWebViewMessage> OnReceivedMessageAsObservable()
        {
            return onReceivedMessage ?? (onReceivedMessage = new Subject<UniWebViewMessage>());
        }

        public IObservable<Unit> OnCloseAsObservable()
        {
            return onClose ?? (onClose = new Subject<Unit>());
        }
    }
}

#endif
