
using UnityEngine.Networking;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Net.WebRequest;

namespace Modules.Net.WebDownload
{
    public class DownloadRequest
    {
        //----- params -----

        //----- field -----

        protected UnityWebRequest request = null;

        protected CancellationTokenSource cancellationTokenSource = null;

        //----- property -----

        /// <summary> リクエストURL. </summary>
        public string Url { get; private set; }

        /// <summary> FilePath. </summary>
        public string FilePath { get; private set; }

        /// <summary> タイムアウト時間(秒). </summary>
        public virtual int TimeOutSeconds { get { return 30; } }

        //----- method -----

        public virtual void Initialize(string url, string filePath)
        {
            Url = url;
            FilePath = filePath;

            cancellationTokenSource = new CancellationTokenSource();
        }

        public async UniTask Download(IProgress<float> progress)
        {
            var handler = new DownloadHandlerFile(FilePath);

            handler.removeFileOnAbort = true;

            request = new UnityWebRequest(Url)
            {
                method = UnityWebRequest.kHttpVerbGET,
                timeout = TimeOutSeconds,
                downloadHandler = handler,
            };

            try
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    if (progress != null)
                    {
                        progress.Report(operation.progress);
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    await UniTask.NextFrame(cancellationToken: cancellationTokenSource.Token);
                }
                
                var isError = request.HasError();
                var isSuccess = request.IsSuccess();

                if (!isSuccess || isError)
                {
                    throw new UnityWebRequestErrorException(request);
                }
            }
            finally
            {
                request.Dispose();
                request = null;
            }
        }

        public void Cancel(bool throwException = false)
        {
            if (request == null) { return; }

            if (cancellationTokenSource != null)
            {
                if(!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource = null;
                }
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

            if (throwException)
            {
                throw new UnityWebRequestErrorException(request);
            }
        }
    }
}
