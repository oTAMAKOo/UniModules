
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections;
using System.Net;
using System.Threading;
using UniRx;

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

        public static IObservable<byte[]> Send(this UnityWebRequest request, IProgress<float> progress = null)
        {
            return Observable.FromMicroCoroutine<byte[]>((observer, cancellation) => Send(observer, cancellation, request, progress));
        }

        private static IEnumerator Send(IObserver<byte[]> observer, CancellationToken cancel, UnityWebRequest request, IProgress<float> progress)
        {
            using (request)
            {
                var error = false;

                var operation = request.SendWebRequest();

                while (!operation.isDone && !cancel.IsCancellationRequested)
                {
                    try
                    {
                        if (progress != null)
                        {
                            progress.Report(operation.progress);
                        }
                    }
                    catch (Exception ex)
                    {
                        error = true;
                        observer.OnError(ex);
                        break;
                    }

                    yield return null;
                }

                if (!cancel.IsCancellationRequested && !error)
                {
                    if (progress != null)
                    {
                        try
                        {
                            progress.Report(request.downloadProgress);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            error = true;
                        }
                    }

                    var isError = request.HasError();
                    var isSuccess = request.IsSuccess();

                    if (!isError && isSuccess && !error)
                    {
                        observer.OnNext(request.downloadHandler != null ? request.downloadHandler.data : null);
                        observer.OnCompleted();
                    }
                    else
                    {
                        observer.OnError(new UnityWebRequestErrorException(request));
                    }
                }
            }
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
