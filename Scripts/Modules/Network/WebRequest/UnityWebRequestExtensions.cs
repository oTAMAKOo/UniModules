
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Modules.Net.WebRequest
{
    public static class UnityWebRequestExtensions
    {
        private static readonly HttpStatusCode[] SuccessStatus = new HttpStatusCode[]
        {
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Accepted,
            HttpStatusCode.NonAuthoritativeInformation,
            HttpStatusCode.NoContent,
            HttpStatusCode.ResetContent,
            HttpStatusCode.PartialContent
        };

        public static async UniTask<byte[]> Send(this UnityWebRequest request, IProgress<float> progress = null, CancellationToken cancelToken = default)
        {
			var bytes = new byte[0];

			if (cancelToken.IsCancellationRequested){ return null; }

			using (request)
            {
				var operation = request.SendWebRequest();

				while (!operation.isDone)
				{
					if (cancelToken.IsCancellationRequested){ break; }

					if (progress != null)
					{
						progress.Report(operation.progress);
					}

					await UniTask.NextFrame(CancellationToken.None);
				}

				if (!cancelToken.IsCancellationRequested)
				{
					if (progress != null)
					{
						progress.Report(request.downloadProgress);
					}

					var isError = request.HasError();
					var isSuccess = request.IsSuccess();

					if (isSuccess && !isError)
					{
						bytes = request.downloadHandler != null ? request.downloadHandler.data : null;
					}
				}
				else
				{
					request.Abort();
				}
			}

			return bytes;
        }
        
        public static bool IsSuccess(this UnityWebRequest unityWebRequest)
        {
            return SuccessStatus.Contains((HttpStatusCode)unityWebRequest.responseCode);
        }

        public static bool HasError(this UnityWebRequest unityWebRequest)
        {
            #if UNITY_2020_2_OR_NEWER

            var result = unityWebRequest.result;

            return result == UnityWebRequest.Result.ConnectionError || 
                   result == UnityWebRequest.Result.DataProcessingError ||
                   result == UnityWebRequest.Result.ProtocolError;

            #else

            return unityWebRequest.isHttpError || unityWebRequest.isNetworkError;
            
            #endif
        }
    }
}
