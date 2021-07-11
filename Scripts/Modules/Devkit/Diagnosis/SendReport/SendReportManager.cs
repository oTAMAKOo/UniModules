
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using Modules.Devkit.Diagnosis.LogTracker;

namespace Modules.Devkit.Diagnosis.SendReport
{
    public interface ISendReportBuilder
    {
        void Build(string screenShotData, string logData);
    }

    public sealed class SendReportManager : Singleton<SendReportManager>
    {
        //----- params -----

        public enum ReportDataFormat
        {
            None = 0,

            Form,
            Json,
        }

        public sealed class LogContainer
        {
            public LogEntry[] contents = null;
        }

        //----- field -----

        private string reportUrl = null;

        private ReportDataFormat format = ReportDataFormat.None;

        private Dictionary<string, string> reportContents = null;

        private ISendReportBuilder[] sendReportBuilders = null;

        private AesCryptKey aesCryptKey = null;

        public byte[] screenShotData = null;
        
        private Subject<Unit> onRequestReport = null;
        private Subject<string> onReportComplete = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        /// <summary> 初期化. </summary>
        public void Initialize(ReportDataFormat format)
        {
            if (initialized){ return; }

            this.format = format;

            reportContents = new Dictionary<string, string>();

            sendReportBuilders = new[] { new DefaultSendReportBuilder(), };

            initialized = true;
        }

        public void SetReportUrl(string reportUrl)
        {
            this.reportUrl = reportUrl;
        }

        public void SetCryptKey(AesCryptKey aesCryptKey)
        {
            this.aesCryptKey = aesCryptKey;
        }

        public void SetReportBuilders(ISendReportBuilder[] sendReportBuilders)
        {
            this.sendReportBuilders = sendReportBuilders;
        }

        public IObservable<Unit> Send(IProgress<float> progressNotifier = null)
        {
            return Observable.FromCoroutine(() => SendReport(progressNotifier));
        }

        private IEnumerator SendReport(IProgress<float> progressNotifier)
        {
            // 送信内容構築.
            BuildPostContent();

            // 送信要求イベント.
            if (onRequestReport != null)
            {
                onRequestReport.OnNext(Unit.Default);
            }

            // 送信.

            var postReportYield = Observable.FromMicroCoroutine<string>(observer => PostReport(observer, progressNotifier)).ToYieldInstruction();

            while (!postReportYield.IsDone)
            {
                yield return null;
            }

            reportContents.Clear();

            // 終了イベント.
            if (onReportComplete != null)
            {
                onReportComplete.OnNext(postReportYield.Result);
            }
        }

        private IEnumerator PostReport(IObserver<string> observer, IProgress<float> progress)
        {
            if (string.IsNullOrEmpty(reportUrl))
            {
                throw new Exception("report url is empty.");
            }

            UnityWebRequest webRequest = null;

            switch (format)
            {
                case ReportDataFormat.Form:
                    webRequest = UnityWebRequest.Post(reportUrl, CreateReportFormSections());
                    break;

                case ReportDataFormat.Json:
                    webRequest = UnityWebRequest.Post(reportUrl, CreateReportJson());
                    break;
            }

            webRequest.timeout = 30;

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                if (progress != null)
                {
                    progress.Report(operation.progress);
                }

                yield return null;
            }

            var errorMessage = string.Empty;

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                errorMessage = string.Format("[{0}]{1}", webRequest.responseCode, webRequest.error);
            }

            observer.OnNext(errorMessage);
            observer.OnCompleted();
        }

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

            foreach (var builder in sendReportBuilders)
            {
                builder.Build(screenShotBase64, logData);
            }
        }

        private string GetReportTextPostData()
        {
            var unityLogTracker = UnityLogTracker.Instance;

            var logs = unityLogTracker.Logs.ToArray();

            if (logs.IsEmpty()) { return string.Empty; }

            var container = new LogContainer() { contents = logs };

            return JsonUtility.ToJson(container);
        }

        /// <summary> 送信情報に追加 </summary>
        public void AddReportContent(string key, string value)
        {
            if (reportContents == null) { return; }

            if (string.IsNullOrEmpty(value))
            {
                value = "---";
            }

            value = aesCryptKey != null ? value.Encrypt(aesCryptKey) : value;

            reportContents.Add(key, value);
        }

        private List<IMultipartFormSection> CreateReportFormSections()
        {
            var reportForm = new List<IMultipartFormSection>();

            foreach (var item in reportContents)
            {
                reportForm.Add(new MultipartFormDataSection(item.Key, item.Value));
            }

            return reportForm;
        }

        private string CreateReportJson()
        {
            return reportContents.ToJson();
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
