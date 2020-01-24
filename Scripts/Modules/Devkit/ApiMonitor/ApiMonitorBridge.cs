
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Networking;

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

        public string url = null;
        public RequestType requestType = RequestType.None;
        public RequestStatus status = RequestStatus.None;
        public ulong retryCount = 0;
        public string result = null;
        public string statusCode = null;
        public Exception exception = null;
    }

    public sealed class ApiMonitorBridge : Singleton<ApiMonitorBridge>
    {
        //----- params -----

        public const int HistoryCount = 100;

        //----- field -----

        private string serverUrl = null;

        private Dictionary<WebRequest, WebRequestInfo> webRequestInfos = null;

        private FixedQueue<WebRequestInfo> requestInfoHistory = null;

        private Subject<Unit> onUpdateInfo = null;

        //----- property -----

        public string ServerUrl { get { return serverUrl; } }

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
            this.serverUrl = serverUrl;
        }

        public void Start(WebRequest webRequest)
        {
            var url = webRequest.HostUrl.Replace(serverUrl, string.Empty);

            var info = new WebRequestInfo()
            {
                url = url,
                requestType = GetRequestType(webRequest),
                status = WebRequestInfo.RequestStatus.Connection,
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

            info.status = WebRequestInfo.RequestStatus.Success;
            info.statusCode = webRequest.StatusCode;
            info.result = result;

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

            info.status = WebRequestInfo.RequestStatus.Retry;
            info.retryCount++;

            if (onUpdateInfo != null)
            {
                onUpdateInfo.OnNext(Unit.Default);
            }
        }

        public void OnRetryLimit(WebRequest webRequest)
        {
            var info = webRequestInfos.GetValueOrDefault(webRequest);

            if (info == null) { return; }

            info.status = WebRequestInfo.RequestStatus.Failure;
            info.statusCode = webRequest.StatusCode;

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

            info.status = WebRequestInfo.RequestStatus.Failure;
            info.statusCode = webRequest.StatusCode;
            info.exception = ex;

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

                info.status = WebRequestInfo.RequestStatus.Cancel;
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
