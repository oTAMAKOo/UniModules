
using UnityEngine;
using System;
using System.IO;
using UniRx;
using Extensions;
using Modules.Net.WebDownload;
using Modules.Net.WebRequest;

namespace Modules.ExternalAssets
{
    public sealed class FileAssetDownloader : FileDownloader<DownloadRequest>
    {
        //----- params -----

		//----- field -----

		private Subject<Exception> onError = null;
		private Subject<string> onTimeout = null;

		//----- property -----

        //----- method -----

		public IObservable<Unit> Download(string url, string downloadDirectory = null, IProgress<float> progress = null)
        {
			if (string.IsNullOrEmpty(downloadDirectory)){ return Observable.ReturnUnit(); }

			if (!Directory.Exists(downloadDirectory))
            {
                Directory.CreateDirectory(downloadDirectory);
            }

			var downloadRequest = SetupDownloadRequest(url, downloadDirectory);
            
            return Download(downloadRequest, progress);
        }

		protected override void OnComplete(DownloadRequest downloadRequest, double totalMilliseconds) { }

        protected override IObservable<RequestErrorHandle> OnError(DownloadRequest downloadRequest, Exception ex)
        {
            var type = ex.GetType();

			if (type == typeof(TimeoutException))
			{
				Debug.LogErrorFormat("DownloadRequest Timeout \n\n[URL]\n{0}\n\n[Exception]\n{1}\n", downloadRequest.Url, ex.StackTrace);

				if (onTimeout != null)
				{
					onTimeout.OnNext(downloadRequest.Url);
				}
			}
			else if (type == typeof(UnityWebRequestErrorException) && ex is UnityWebRequestErrorException)
			{
				var exception = (UnityWebRequestErrorException)ex;
				var errorMessage = exception.RawErrorMessage;

				Debug.LogErrorFormat("DownloadRequest Error : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", errorMessage, downloadRequest.Url, ex.StackTrace);

				if (onError != null)
				{
					onError.OnNext(ex);
				}
			}
			else
			{
				Debug.LogErrorFormat("DownloadRequest UnknownError : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", ex.Message, downloadRequest.Url, ex.StackTrace);

				if (onError != null)
				{
					onError.OnNext(ex);
				}
			}

            return Observable.Return(RequestErrorHandle.Retry);
        }

        protected override void OnRetryLimit(DownloadRequest downloadRequest)
        {
			var ex = new Exception($"DownloadRequest RetryLimit : [URL]\n{downloadRequest.Url}");

			if (onError != null)
			{
				onError.OnNext(ex);
			}
        }

		public IObservable<Exception> OnErrorAsObservable()
		{
			return onError ?? (onError = new Subject<Exception>());
		}

		public IObservable<string> OnTimeoutAsObservable()
		{
			return onTimeout ?? (onTimeout = new Subject<string>());
		}
	}
}