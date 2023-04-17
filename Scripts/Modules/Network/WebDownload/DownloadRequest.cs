
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
		}

        public async UniTask Download(IProgress<float> progress, CancellationToken cancelToken = default)
        {
			var handler = new DownloadHandlerFile(FilePath)
			{
				removeFileOnAbort = true,
			};

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

                    await UniTask.NextFrame(cancellationToken: cancelToken);

					if (cancelToken.IsCancellationRequested) { break; }
				}

				if (request != null)
				{
					if (!request.IsSuccess() || request.HasError())
					{
						throw new UnityWebRequestErrorException(request);
					}
				}
            }
			catch (OperationCanceledException)
			{
				if (request != null)
				{
					request.Abort();
				}
			}
			finally
            {
				if (request != null)
				{
					request.Dispose();
					request = null;
				}
            }
        }

        public void Cancel()
        {
            if (request == null) { return; }

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
