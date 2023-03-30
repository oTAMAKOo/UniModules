

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using UniRx;
using Modules.Devkit.Console;
using Modules.Net.WebRequest;

namespace Modules.Net.WebDownload
{
	public sealed class TextureWebDownload
	{
        //----- params -----

	    public static readonly string ConsoleEventName = "Download";
	    public static readonly Color ConsoleEventColor = new Color(0.85f, 0.88f, 0f);

        private const float DefaultTimeOutSeconds = 5f;
        private const int DefaultRetryCount = 3;
        private const float DefaultRetryDelaySeconds = 5f;

        //----- field -----

        private UnityWebRequest currentRequest = null;

        private float timeOutSeconds = DefaultTimeOutSeconds;
        private int retryCount = DefaultRetryCount;
        private float retryDelaySeconds = DefaultRetryDelaySeconds;

        private Subject<Unit> onError = null;
        private Subject<Unit> onTimeout = null;
        private Subject<Texture2D> onComplete = null;

        //----- property -----

        // タイムアウトするまでの時間(秒).
        public float TimeOutSeconds
	    {
	        get { return timeOutSeconds; }
            set { timeOutSeconds = value; }
	    }

        // リトライ回数.
	    public int RetryCount
	    {
            get { return retryCount; }
            set { retryCount = value; }
        }

        // リトライするまでの時間(秒).
	    public float RetryDelaySeconds
	    {
            get { return retryDelaySeconds; }
            set { retryDelaySeconds = value; }
        }

        //----- method -----

        public IObservable<Texture2D> Download(string url)
        {
            if(currentRequest != null)
            {
                Cancel(false);
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                return Observable.FromCoroutine<Texture2D>(observer => DownloadCore(observer, url))
                   // タイムアウト時間設定.
                   .Timeout(TimeSpan.FromSeconds(TimeOutSeconds))
                   // リトライ処理.
                   .OnErrorRetry((TimeoutException ex) => OnTimeout(ex), RetryCount, TimeSpan.FromSeconds(RetryDelaySeconds))
                   // エラー処理.
                   .DoOnError(ex =>
                       {
                           OnError(ex);
                           Cancel();
                       })
                   // 正常終処理.
                   .Do(x =>
                       {
                           if (x != null)
                           {
                               sw.Stop();
                               OnComplete(x, sw.Elapsed.TotalMilliseconds);
                           }
                       })
                   // 通信完了.
                   .Finally(() => currentRequest = null);
            }
            catch (Exception)
            {
                currentRequest = null;

                return Observable.Return<Texture2D>(null);
            }
        }

        public void Cancel(bool throwException = false)
        {
            currentRequest.Abort();

            if (throwException)
            {
                throw new UnityWebRequestErrorException(currentRequest);
            }
        }

        private IEnumerator DownloadCore(IObserver<Texture2D>  observer, string url)
        {
            currentRequest = UnityWebRequestTexture.GetTexture(url);

            yield return currentRequest.SendWebRequest();

            if (currentRequest.result == UnityWebRequest.Result.ConnectionError || currentRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                observer.OnError(new Exception(currentRequest.error));
                yield break;
            }

            var texture = ((DownloadHandlerTexture)currentRequest.downloadHandler).texture;

            observer.OnNext(texture);
            observer.OnCompleted();
        }

        private void OnTimeout(Exception ex)
        {
            var type = ex.GetType();

            if (type == typeof(TimeoutException))
            {
                var message = string.Format("WebRequest Retry\n{0}\n", currentRequest.url);

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }
            else
            {
				Cancel();

                if (onTimeout != null)
                {
                    onError.OnNext(Unit.Default);
                }
				else
				{
					var message = string.Format("WebRequest TimeoutError\n{0}\n{1}\n", currentRequest.url, ex.Message);

					UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
				}
            }
        }

        private void OnError(Exception ex)
        {
            var type = ex.GetType();

            if (type == typeof(TimeoutException))
            {
				if (onTimeout != null)
                {
                    onTimeout.OnNext(Unit.Default);
                }
				else
				{
					Debug.LogErrorFormat("WebRequest Timeout \n\n[URL]\n{0}\n\n[Exception]\n{1}\n", currentRequest.url, ex.StackTrace);
				}
            }
            else if (type == typeof(UnityWebRequestErrorException) && ex is UnityWebRequestErrorException)
            {
				if (onError != null)
                {
                    onError.OnNext(Unit.Default);
                }
				else
				{
					var exception = (UnityWebRequestErrorException)ex;
					var errorMessage = exception.RawErrorMessage;

					Debug.LogErrorFormat("WebRequest Error : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", errorMessage, currentRequest.url, ex.StackTrace);
				}
            }
            else
            {
				if (onError != null)
                {
                    onError.OnNext(Unit.Default);
                }
				else
				{
					Debug.LogErrorFormat("WebRequest UnknownError : {0}\n\n[URL]\n{1}\n\n[Exception]\n{2}\n", ex.Message, currentRequest.url, ex.StackTrace);
				}
            }
        }

        private void OnComplete(Texture2D texture, double totalMilliseconds)
        {
            if (Debug.isDebugBuild)
            {
                var builder = new StringBuilder();

                builder.AppendFormat("URL: {0} ({1:F1}ms)", currentRequest.url, totalMilliseconds).AppendLine();
                builder.AppendLine();

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
            }

            if (onComplete != null)
            {
                onComplete.OnNext(texture);
            }
        }

        public IObservable<Unit> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Unit>());
        }

        public IObservable<Unit> OnTimeoutAsObservable()
        {
            return onTimeout ?? (onTimeout = new Subject<Unit>());
        }

        public IObservable<Texture2D> OnCompleteAsObservable()
        {
            return onComplete ?? (onComplete = new Subject<Texture2D>());
        }
    }
}
