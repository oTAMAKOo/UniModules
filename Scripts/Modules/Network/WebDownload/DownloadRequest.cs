
using UnityEngine.Networking;
using System;
using System.IO;
using UniRx;
using Extensions;
using Modules.Net.WebRequest;

namespace Modules.Net.WebDownload
{
    public class DownloadRequest
    {
        //----- params -----

        //----- field -----

        protected UnityWebRequest request = null;
        protected byte[] buffer = null;

        //----- property -----

        /// <summary> リクエストURL. </summary>
        public string Url { get; private set; }

        /// <summary> FilePath. </summary>
        public string FilePath { get; private set; }

        /// <summary> タイムアウト時間(秒). </summary>
        public virtual int TimeOutSeconds { get { return 30; } }

        //----- method -----

        public virtual void Initialize(string url, string downloadDirectory)
        {
            Url = url;
            FilePath = PathUtility.Combine(downloadDirectory, Path.GetFileName(url));

            if (buffer == null)
            {
                buffer = new byte[256 * 1024];
            }
        }

        public IObservable<Unit> Download(IProgress<float> progress)
        {
            request = new UnityWebRequest(Url)
            {
                timeout = TimeOutSeconds,
                downloadHandler = new FileDownloadHandler(FilePath, buffer),
            };

            Action onFinally = () =>
            {
                request.Dispose();
                request = null;
            };

            return request.Send(progress).Finally(onFinally).AsUnitObservable();
        }

        public void Cancel(bool throwException = false)
        {
            if (request == null) { return; }

            if (throwException)
            {
                throw new UnityWebRequestErrorException(request);
            }

            try
            {
                request.Abort();
            }
            finally
            {
                request.Dispose();
                request = null;
            }
        }
    }
}
