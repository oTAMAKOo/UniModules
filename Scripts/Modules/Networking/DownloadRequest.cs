
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using UniRx;
using Extensions;

namespace Modules.Networking
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

        private void BuildUnityWebRequest()
        {
            request = new UnityWebRequest(Url);

            request.timeout = TimeOutSeconds;

            request.downloadHandler = new FileDownloadHandler(FilePath, buffer);
        }

        public IObservable<Unit> Download(IProgress<float> progress)
        {
            BuildUnityWebRequest();

            return request.Send(progress).AsUnitObservable();
        }
        
        public void Cancel(bool throwException = false)
        {
            request.Abort();

            if (throwException)
            {
                throw new UnityWebRequestErrorException(request);
            }
        }
    }
}
