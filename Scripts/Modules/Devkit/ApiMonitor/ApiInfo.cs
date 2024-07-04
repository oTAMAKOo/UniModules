
using System;

namespace Modules.Net.WebRequest
{
    public class ApiInfo
    {
        //----- params -----

        public enum RequestStatus
        {
            None = 0,

            Connection,
            Success,
            Failure,
            Retry,
            Cancel,
        }

        //----- field -----

        private string headers = null;

        private string uriParams = null;

        private string body = null;

        //----- property -----

        public int Id { get; private set; }

        public IWebRequestClient WebRequest { get; private set; }

        public Method Method { get { return WebRequest.Method; } }

        public DateTime? Start { get; set; }

        public DateTime? Finish { get; set; }

        public string Url { get; set; }

        public RequestStatus Status { get; set; }

        public ulong RetryCount { get; set; }

        public string Result { get; set; }

        public string StatusCode { get; set; }

        public double? ElapsedTime { get; set; }

        public Exception Exception { get; set; }

        public string StackTrace { get; set; }

        //----- method -----

        public ApiInfo(int id, IWebRequestClient webRequest)
        {
            Id = id;
            WebRequest = webRequest;
        }

        public string GetHeaders()
        {
            if (!string.IsNullOrEmpty(headers)){ return headers; }

            headers = WebRequest.GetHeaderString();

            return headers;
        }

        public string GetUriParams()
        {
            if (!string.IsNullOrEmpty(uriParams)){ return uriParams; }

            uriParams = WebRequest.GetUrlParamsString();

            return uriParams;
        }

        public string GetBody()
        {
            if (!string.IsNullOrEmpty(body)){ return body; }

            body = WebRequest.GetBodyString();

            return body;
        }
    }
}
