
using UnityEngine;
using System;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Networking
{
    public sealed class ApiTracker : Singleton<ApiTracker>
    {
        //----- params -----

        public const int HistoryCount = 100;

        //----- field -----

        private Dictionary<IWebRequest, ApiInfo> apiInfos = null;

        private FixedQueue<ApiInfo> apiInfoHistory = null;

        private Subject<Unit> onUpdateInfo = null;

        private int currentTrackerId = 0;

        //----- property -----

        public string ServerUrl { get; private set; }

        //----- method -----

        protected override void OnCreate()
        {
            apiInfos = new Dictionary<IWebRequest, ApiInfo>();
            apiInfoHistory = new FixedQueue<ApiInfo>(HistoryCount);
        }

        public ApiInfo[] GetHistory()
        {
            return apiInfoHistory.ToArray();
        }

        public void SetServerUrl(string serverUrl)
        {
            this.ServerUrl = serverUrl;
        }

        public void Start(IWebRequest webRequest)
        {
            var url = webRequest.HostUrl.Replace(ServerUrl, string.Empty);

            var info = new ApiInfo(currentTrackerId++)
            {
                Url = url,
                Request = GetRequestType(webRequest),
                Headers = webRequest.GetHeaderString(),
                UriParams = webRequest.GetUrlParamsString(),
                Body = webRequest.GetBodyString(),
                Status = ApiInfo.RequestStatus.Connection,
                StackTrace = StackTraceUtility.ExtractStackTrace(),
                Start = DateTime.Now,
            };

            apiInfos.Add(webRequest, info);

            apiInfoHistory.Enqueue(info);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnComplete(IWebRequest webRequest, string result, double elapsedTime)
        {
            var info = apiInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Finish = DateTime.Now;
            info.ElapsedTime = elapsedTime;
            info.Status = ApiInfo.RequestStatus.Success;
            info.StatusCode = webRequest.StatusCode;
            info.Result = result;

            apiInfos.Remove(webRequest);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnRetry(IWebRequest webRequest)
        {
            var info = apiInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Status = ApiInfo.RequestStatus.Retry;
            info.RetryCount++;

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnRetryLimit(IWebRequest webRequest)
        {
            var info = apiInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Status = ApiInfo.RequestStatus.Failure;
            info.StatusCode = webRequest.StatusCode;

            apiInfos.Remove(webRequest);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnError(IWebRequest webRequest, Exception ex)
        {
            var info = apiInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Status = ApiInfo.RequestStatus.Failure;
            info.StatusCode = webRequest.StatusCode;
            info.Exception = ex;

            apiInfos.Remove(webRequest);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnForceCancelAll()
        {
            foreach (var webRequestInfo in apiInfos)
            {
                var info = webRequestInfo.Value;

                info.Status = ApiInfo.RequestStatus.Cancel;
            }

            apiInfos.Clear();

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void Clear()
        {
            apiInfoHistory.Clear();
            apiInfos.Clear();

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        private ApiInfo.RequestType GetRequestType(IWebRequest webRequest)
        {
            var requestType = ApiInfo.RequestType.None;

            switch (webRequest.Method)
            {
                case "GET":
                    requestType = ApiInfo.RequestType.Get;
                    break;
                case "POST":
                    requestType = ApiInfo.RequestType.Post;
                    break;
                case "PUT":
                    requestType = ApiInfo.RequestType.Put;
                    break;
                case "DELETE":
                    requestType = ApiInfo.RequestType.Delete;
                    break;
            }

            return requestType;
        }

        public IObservable<Unit> OnUpdateInfoAsObservable()
        {
            return onUpdateInfo ?? (onUpdateInfo = new Subject<Unit>());
        }
    }
}
