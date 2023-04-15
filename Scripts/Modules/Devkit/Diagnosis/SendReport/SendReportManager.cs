
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Diagnosis.LogTracker;
using Modules.Net.WebRequest;

namespace Modules.Devkit.Diagnosis.SendReport
{
    public sealed class SendReportResult
    {
        public long ResponseCode { get; private set; }
			
        public string Text { get; private set; }
			
        public byte[] Bytes { get; private set; }
			
        public string Error { get; private set; }

        public bool HasError { get; private set; }

        public SendReportResult(UnityWebRequest request)
        {
            ResponseCode = request.responseCode;
            Text = request.downloadHandler.text;
            Bytes = request.downloadHandler.data;
            Error = request.error;
            HasError = request.HasError();
        }
    }

	public interface ISendReportUploader
	{
		UniTask<SendReportResult> Upload(string reportTitle, Dictionary<string, string> reportContents, IProgress<float> progress, CancellationToken cancelToken);
	}

    public interface ISendReportBuilder
    {
        void Build(string screenShotData, string logData);
    }

    public sealed class SendReportManager : Singleton<SendReportManager>
    {
        //----- params -----

        public sealed class LogContainer 
        {
            public LogEntry[] contents = null;
        }

        //----- field -----

        private Dictionary<string, string> reportContents = null;

        private ISendReportUploader uploader = null;

        private ISendReportBuilder sendReportBuilder = null;

        private AesCryptoKey aesCryptoKey = null;

        public byte[] screenShotData = null;
        
        private Subject<Unit> onRequestReport = null;
        private Subject<SendReportResult> onReportComplete = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        /// <summary> 初期化. </summary>
        public void Initialize(ISendReportUploader uploader)
        {
            if (initialized){ return; }

            this.uploader = uploader;

            reportContents = new Dictionary<string, string>();

            sendReportBuilder = new DefaultSendReportBuilder();

            initialized = true;
        }

        public void SetCryptKey(AesCryptoKey aesCryptoKey)
        {
            this.aesCryptoKey = aesCryptoKey;
        }

        public void SetReportBuilder(ISendReportBuilder sendReportBuilder)
        {
            this.sendReportBuilder = sendReportBuilder;
        }

        public async UniTask Send(string reportTitle, IProgress<float> progressNotifier = null, CancellationToken cancelToken = default)
        {
			// 送信内容構築.
            BuildPostContent();

            // 送信要求イベント.
            if (onRequestReport != null)
            {
                onRequestReport.OnNext(Unit.Default);
            }

            // 送信.

            SendReportResult result = null;

			try
			{
				result = await uploader.Upload(reportTitle, reportContents, progressNotifier, cancelToken);

				if (result != null && result.HasError)
				{
					Debug.LogErrorFormat("[{0}]{1}", result.ResponseCode, result.Error);
				}

				reportContents.Clear();
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			catch (Exception e)
			{
                Debug.LogException(e);
			}

			// 終了イベント.
            if (onReportComplete != null)
            {
                onReportComplete.OnNext(result);
            }
        }

        // ※ UniTaskだとWaitForEndOfFrameのタイミングが正しく取得できないのでコルーチンで制御する.
        public IEnumerator CaptureScreenShot()
        {
            yield return new WaitForEndOfFrame();

            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();

            screenShotData = tex.EncodeToPNG();

            UnityUtility.SafeDelete(tex);
        }

        private void BuildPostContent()
        {
            reportContents.Clear();

            //------ ScreenShot ------

            var screenShotBase64 = Convert.ToBase64String(screenShotData);

            // スクリーンショット情報破棄.
            screenShotData = null;

            //------ Log ------

            var logData = GetReportTextPostData();

            //------ ReportContent ------

            sendReportBuilder.Build(screenShotBase64, logData);
        }

        private string GetReportTextPostData()
        {
            var unityLogTracker = UnityLogTracker.Instance;

            var logs = unityLogTracker.Logs.ToArray();

            return logs.Any() ? 
                   JsonUtility.ToJson(new LogContainer { contents = logs }) : 
                   JsonUtility.ToJson(string.Empty);
        }

        /// <summary> 送信情報に追加 </summary>
        public void AddReportContent(string key, string value)
        {
            if (reportContents == null) { return; }

            if (string.IsNullOrEmpty(value))
            {
                value = "---";
            }

            value = aesCryptoKey != null ? value.Encrypt(aesCryptoKey) : value;

            reportContents.Add(key, value);
        }

        public IObservable<Unit> OnRequestReportAsObservable()
        {
            return onRequestReport ?? (onRequestReport = new Subject<Unit>());
        }

        public IObservable<SendReportResult> OnReportCompleteAsObservable()
        {
            return onReportComplete ?? (onReportComplete = new Subject<SendReportResult>());
        }
    }
}
