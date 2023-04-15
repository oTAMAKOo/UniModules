
using UnityEngine;
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Net.WebDownload;
using Modules.Net.WebRequest;

namespace Modules.ExternalAssets
{
    public sealed class FileAssetDownLoader : FileDownLoader<DownloadRequest>
    {
        //----- params -----

		//----- field -----

		private Subject<Exception> onError = null;
		private Subject<string> onTimeout = null;

		//----- property -----

        //----- method -----

		public async UniTask Download(string url, string filePath, IProgress<float> progress = null, CancellationToken cancelToken = default)
		{
			try
			{
				var directory = Directory.GetParent(filePath);

				if (!directory.Exists)
				{
					directory.Create();
				}

				var downloadRequest = SetupDownloadRequest(url, filePath);

				await Download(downloadRequest, progress, cancelToken);
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
		}

		protected override void OnComplete(DownloadRequest downloadRequest, double totalMilliseconds) { }

        protected override UniTask<RequestErrorHandle> OnError(DownloadRequest downloadRequest, Exception ex, CancellationToken cancelToken = default)
        {
            var type = ex.GetType();

			if (type == typeof(TimeoutException))
			{
				if (onTimeout != null)
				{
					onTimeout.OnNext(downloadRequest.Url);
				}
				else
				{
					Debug.LogErrorFormat("DownloadRequest Timeout \n\n[URL]\n{0}\n\n[Exception]\n{1}\n", downloadRequest.Url, ex.StackTrace);
				}
			}
			else if (type == typeof(UnityWebRequestErrorException) && ex is UnityWebRequestErrorException)
			{
				if (onError != null)
				{
					onError.OnNext(ex);
				}
				else
				{
					var exception = (UnityWebRequestErrorException)ex;
					var errorMessage = exception.RawErrorMessage;

					Debug.LogErrorFormat("DownloadRequest Error : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", errorMessage, downloadRequest.Url, ex.StackTrace);
				}
			}
			else
			{
				if (onError != null)
				{
					onError.OnNext(ex);
				}
				else
				{
					Debug.LogErrorFormat("DownloadRequest UnknownError : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", ex.Message, downloadRequest.Url, ex.StackTrace);
				}
			}

            return UniTask.FromResult(RequestErrorHandle.Retry);
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