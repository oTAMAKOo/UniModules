
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Diagnosis.LogTracker;

namespace Modules.Devkit.Diagnosis.SendReport
{
	public interface ISendReportUploader
	{
		UniTask<string> Upload(string reportTitle, Dictionary<string, string> reportContents, IProgress<float> progress);
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
        private Subject<string> onReportComplete = null;

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

        public async UniTask Send(string reportTitle, IProgress<float> progressNotifier = null)
        {
			// 送信内容構築.
            BuildPostContent();

            // 送信要求イベント.
            if (onRequestReport != null)
            {
                onRequestReport.OnNext(Unit.Default);
            }

            // 送信.

			var completeMessage = string.Empty;

			try
			{
				completeMessage = await uploader.Upload(reportTitle, reportContents, progressNotifier);

				reportContents.Clear();
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				completeMessage = e.Message;
			}

			// 終了イベント.
            if (onReportComplete != null)
            {
                onReportComplete.OnNext(completeMessage);
            }
        }

        public void CaptureScreenShot()
        {
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

        public IObservable<string> OnReportCompleteAsObservable()
        {
            return onReportComplete ?? (onReportComplete = new Subject<string>());
        }
    }
}
