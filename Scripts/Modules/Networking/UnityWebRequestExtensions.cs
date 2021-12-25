﻿﻿
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections;
using System.Net;
using System.Threading;
using UniRx;

namespace Modules.Networking
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

                    var isError = request.result == UnityWebRequest.Result.ConnectionError;
                    var isSuccess = SuccessStatus.Contains((HttpStatusCode)request.responseCode);

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
    }
}
