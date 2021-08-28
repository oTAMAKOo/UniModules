
using System;
using Extensions;

namespace Modules.Networking
{
    public class ApiInfo
    {
        //----- params -----

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

        //----- field -----

        //----- property -----

        public int Id { get; private set; }

        public DateTime? Start { get; set; }

        public DateTime? Finish { get; set; }

        public string Url { get; set; }

        public RequestType Request { get; set; }

        public RequestStatus Status { get; set; }

        public string Headers { get; set; }

        public string UriParams { get; set; }

        public string Body { get; set; }

        public ulong RetryCount { get; set; }

        public string Result { get; set; }

        public string StatusCode { get; set; }

        public double? ElapsedTime { get; set; }

        public Exception Exception { get; set; }

        public string StackTrace { get; set; }

        //----- method -----

        public ApiInfo(int id)
        {
            Id = id;
        }
    }
}
