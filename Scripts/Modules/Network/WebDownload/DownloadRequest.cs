
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

            var abort = false;

            try
            {
                using (var webRequest = UnityWebRequest.Get(Url))
                {
                    webRequest.timeout = TimeOutSeconds;

                    using (var downloadHandlerFile = new DownloadHandlerFile(FilePath))
                    {
                        downloadHandlerFile.removeFileOnAbort = true;

                        webRequest.downloadHandler = downloadHandlerFile;

                        var operation = webRequest.SendWebRequest();

                        while (!operation.isDone)
                        {
                            if (!abort && (canceled || cancelToken.IsCancellationRequested))
                            {
                                webRequest.Abort();
                                abort = true;
                            }

                            if (progress != null)
                            {
                                progress.Report(operation.progress);
                            }

                            await UniTask.NextFrame(CancellationToken.None);
                        }

                        if (webRequest != null)
                        {
                            if (!webRequest.IsSuccess() || webRequest.HasError())
                            {
                                throw new UnityWebRequestErrorException(webRequest);
                            }
                        }
                    }
                }
            }
            catch
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            finally
            {
                if (abort)
                {
                    if (File.Exists(FilePath))
                    {
                        File.Delete(FilePath);
                    }
                }
            }
        }

        public void Cancel()
        {
            canceled = true;
        }

        protected virtual void OnDispose() { }
    }
}
