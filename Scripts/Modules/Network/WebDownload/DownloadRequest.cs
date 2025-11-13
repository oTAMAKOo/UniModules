
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Net.WebRequest;

namespace Modules.Net.WebDownload
{
    public class DownloadRequest : IDisposable
    {
        //----- params -----

        private const int DefaultTimeOutSeconds = 30;

        //----- field -----

        protected bool canceled = false;

        //----- property -----

        /// <summary> リクエストURL. </summary>
        public string Url { get; private set; }

        /// <summary> FilePath. </summary>
        public string FilePath { get; private set; }

        /// <summary> タイムアウト時間(秒). </summary>
        public int TimeOutSeconds { get; set; } = DefaultTimeOutSeconds;

        public bool IsDisposed { get; private set; }

        //----- method -----

        public DownloadRequest()
        {
            IsDisposed = false;
        }

        ~DownloadRequest()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (IsDisposed){ return; }

            IsDisposed = true;

            OnDispose();

            GC.SuppressFinalize(this);
        }

        public virtual void Initialize(string url, string filePath)
        {
            Url = url;
            FilePath = filePath;
        }

        public virtual async UniTask Download(IProgress<float> progress = null, CancellationToken cancelToken = default)
        {
            canceled = false;

            if (string.IsNullOrEmpty(Url))
            {
                throw new ArgumentNullException(nameof(Url));
            }

            if (string.IsNullOrEmpty(FilePath))
            {
                throw new ArgumentNullException(nameof(FilePath));
            }

            var timestamp = $"t={ DateTime.Now.ToUnixTime(UnixTimeConvert.Milliseconds) }";

            var url = Url.Contains("?") ? $"{ Url }&{ timestamp }" : $"{ Url }?{ timestamp }";

            using var webRequest = UnityWebRequest.Get(url);

            try
            {
                var abort = false;

                webRequest.timeout = TimeOutSeconds;
 
                using var downloadHandlerFile = new DownloadHandlerFile(FilePath)
                {
                    removeFileOnAbort = true
                };
    
                webRequest.downloadHandler = downloadHandlerFile;

                var operation = webRequest.SendWebRequest();

                while (!operation.isDone)
                {
                    if (!abort && (canceled || cancelToken.IsCancellationRequested))
                    {
                        webRequest.Abort();
                        abort = true;
                        break;
                    }

                    if (progress != null)
                    {
                        progress.Report(webRequest.downloadProgress);
                    }

                    await UniTask.NextFrame(CancellationToken.None);
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new UnityWebRequestErrorException(webRequest);
                }

                if (progress != null)
                {
                    progress.Report(1);
                }
            }
            catch (OperationCanceledException)
            {
                SafeDelete(FilePath);

                throw;
            }
            catch
            {
                // ネットワーク/HTTP失敗等 → 部分ファイル破棄.

                SafeDelete(FilePath);

                throw;
            }
        }

        private void SafeDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch 
            {
                /* 処理なし */
            }
        }

        public void Cancel()
        {
            canceled = true;
        }

        protected virtual void OnDispose() { }
    }
}
