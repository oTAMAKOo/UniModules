
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Modules.WebView
{
    public static class HtmlDownloadHelper
    {
        //----- params -----

        private const int DefaultTimeOutSeconds = 5;

        private const int DefaultRetryCount = 3;

        public sealed class PostData
        {
            public string fieldName = string.Empty;
            public string value = string.Empty;
        }

        //----- field -----

        private static Subject<Unit> onDownloadTimeOut = null;
        private static Subject<string> onDownloadError = null;
        
        //----- property -----

        public static int TimeOutSeconds { get; set; } = DefaultTimeOutSeconds;

        public static int RetryCount { get; set; } = DefaultRetryCount;

        //----- method -----

        public static async UniTask<string> Download(string url, PostData[] postDataList)
        {
            var form = new List<IMultipartFormSection>();

            if (postDataList != null)
            {
                foreach (var postData in postDataList)
                {
                    form.Add(new MultipartFormDataSection(postData.fieldName, postData.value));
                }
            }

            var webRequest = UnityWebRequest.Post(url, form);

            var retryCount = 0;

            var requestTime = Time.time;

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                if (Time.time - requestTime < TimeOutSeconds)
                {
                    await UniTask.DelayFrame(1);
                }
                // タイムアウト.
                else
                {
                    if (RetryCount <= retryCount)
                    {
                        if (onDownloadTimeOut != null)
                        {
                            onDownloadTimeOut.OnNext(Unit.Default);
                        }

                        return null;
                    }

                    retryCount++;
                }
            }
            
            // エラー.
            if (!string.IsNullOrEmpty(webRequest.error))
            {
                if (onDownloadError != null)
                {
                    onDownloadError.OnNext(webRequest.error);
                }

                return null;
            }

            return webRequest.downloadHandler.text;
        }
    }
}