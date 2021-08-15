
using UnityEngine;
using System;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Networking
{
    public sealed class WebRequestInfo
    {
        public enum RequestType
        {
            None,

            [Label("POST")]
            Post,
            [Label("PUT")]
            Put,
            [Label("GET")]
            Get,
            [Label("DELETE")]
            Delete,
        }

        public enum RequestStatus
        {
            None = 0,

            Connection,
            Success,
            Failure,
            Retry,
            Cancel,
        }

        public int Id { get; private set; }

        public DateTime Time { get; private set; }

        public string Url { get; set; }
        public RequestType Request { get; set; }
        public RequestStatus Status { get; set; }
        public ulong RetryCount { get; set; }
        public string Result { get; set; }
        public string StatusCode { get; set; }
        public Exception Exception { get; set; }
        public string StackTrace { get; set; }

        public WebRequestInfo(int id)
        {
            Id = id;
            Time = DateTime.Now;
        }
    }

    public sealed class ApiTracker : Singleton<ApiTracker>
    {
        //----- params -----

        public const int HistoryCount = 100;

        //----- field -----

        private Dictionary<WebRequest, WebRequestInfo> webRequestInfos = null;

        private FixedQueue<WebRequestInfo> requestInfoHistory = null;

        private Subject<Unit> onUpdateInfo = null;

        private int currentTrackerId = 0;

        //----- property -----

        public string ServerUrl { get; private set; }

        //----- method -----

        protected override void OnCreate()
        {
            webRequestInfos = new Dictionary<WebRequest, WebRequestInfo>();
            requestInfoHistory = new FixedQueue<WebRequestInfo>(HistoryCount);
        }

        public WebRequestInfo[] GetHistory()
        {
            return requestInfoHistory.ToArray();
        }

        public void SetServerUrl(string serverUrl)
        {
            this.ServerUrl = serverUrl;
        }

        public void Start(WebRequest webRequest)
        {
            var url = webRequest.HostUrl.Replace(ServerUrl, string.Empty);

            var info = new WebRequestInfo(currentTrackerId++)
            {
                Url = url,
                Request = GetRequestType(webRequest),
                Status = WebRequestInfo.RequestStatus.Connection,
                StackTrace = StackTraceUtility.ExtractStackTrace(),
            };

            webRequestInfos.Add(webRequest, info);

            requestInfoHistory.Enqueue(info);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnComplete(WebRequest webRequest, string result)
        {
            var info = webRequestInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Status = WebRequestInfo.RequestStatus.Success;
            info.StatusCode = webRequest.StatusCode;
            info.Result = result;

            webRequestInfos.Remove(webRequest);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnRetry(WebRequest webRequest)
        {
            var info = webRequestInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Status = WebRequestInfo.RequestStatus.Retry;
            info.RetryCount++;

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnRetryLimit(WebRequest webRequest)
        {
            var info = webRequestInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Status = WebRequestInfo.RequestStatus.Failure;
            info.StatusCode = webRequest.StatusCode;

            webRequestInfos.Remove(webRequest);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnError(WebRequest webRequest, Exception ex)
        {
            var info = webRequestInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.Status = WebRequestInfo.RequestStatus.Failure;
            info.StatusCode = webRequest.StatusCode;
            info.Exception = ex;

            webRequestInfos.Remove(webRequest);

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnForceCancelAll()
        {
            foreach (var webRequestInfo in webRequestInfos)
            {
                var info = webRequestInfo.Value;

                info.Status = WebRequestInfo.RequestStatus.Cancel;
            }

            webRequestInfos.Clear();

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void Clear()
        {
            requestInfoHistory.Clear();
            webRequestInfos.Clear();

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        private WebRequestInfo.RequestType GetRequestType(WebRequest webRequest)
        {
            var requestType = WebRequestInfo.RequestType.None;

            switch (webRequest.Method)
            {
                case "GET":
                    requestType = WebRequestInfo.RequestType.Get;
                    break;
                case "POST":
                    requestType = WebRequestInfo.RequestType.Post;
                    break;
                case "PUT":
                    requestType = WebRequestInfo.RequestType.Put;
                    break;
                case "DELETE":
                    requestType = WebRequestInfo.RequestType.Delete;
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
