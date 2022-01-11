
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

        public sealed class LogContainer
        {
            public LogEntry[] contents = null;
        }

        //----- field -----

        private string reportUrl = null;

        private Dictionary<string, string> reportContents = null;

        private ISendReportBuilder sendReportBuilder = null;

        private AesCryptoKey aesCryptoKey = null;

        public byte[] screenShotData = null;
        
        private Subject<Unit> onRequestReport = null;
        private Subject<string> onReportComplete = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        /// <summary> 初期化. </summary>
        public void Initialize()
        {
            if (initialized){ return; }

            reportContents = new Dictionary<string, string>();

            sendReportBuilder = new DefaultSendReportBuilder();

            initialized = true;
        }

        public void SetReportUrl(string reportUrl)
        {
            this.reportUrl = reportUrl;
        }

        public void SetCryptKey(AesCryptoKey aesCryptoKey)
        {
            this.aesCryptoKey = aesCryptoKey;
        }

        public void SetReportBuilder(ISendReportBuilder sendReportBuilder)
        {
            this.sendReportBuilder = sendReportBuilder;
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

            var completeMessage = postReportYield.HasError ? postReportYield.Error.Message : postReportYield.Result;

            // 終了イベント.
            if (onReportComplete != null)
            {
                onReportComplete.OnNext(completeMessage);
            }
        }

        private IEnumerator PostReport(IObserver<string> observer, IProgress<float> progress)
        {
            if (string.IsNullOrEmpty(reportUrl))
            {
                throw new Exception("report url is empty.");
            }

            var reportJson = CreateReportJson();
            
            var webRequest = UnityWebRequest.Post(reportUrl, reportJson);

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

            var hasError = false;

            #if UNITY_2020_1_OR_NEWER

            hasError = webRequest.result == UnityWebRequest.Result.ConnectionError || 
                       webRequest.result == UnityWebRequest.Result.ProtocolError;
            
            #else

            hasError = webRequest.isNetworkError || webRequest.isHttpError;

            #endif

            if(hasError)
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
