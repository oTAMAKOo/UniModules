
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using UniRx;
using Extensions;

#if ENABLE_SRDEBUGGER

using SRDebugger.Internal;

#endif

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public abstract class SendReportSheetController : MonoBehaviour
    {
        #if ENABLE_SRDEBUGGER

        //----- params -----

        //----- field -----

        [SerializeField]
        private Text reportContentText = null;
        [SerializeField]
        private Button sendReportButton = null;
        [SerializeField]
        private Text sendReportButtonText = null;
        [SerializeField]
        private InputField commentInputField = null;
        [SerializeField]
        private Slider progressBar = null;
        [SerializeField]
        private Text progressBarText = null;

        private List<IMultipartFormSection> reportForm = null;

        private LogEntry[] reportContents = null;

        private IDisposable sendReportDisposable = null;

        private AesManaged aesManaged = null;

        protected bool initialized = false;

        //----- property -----

        public abstract string PostReportURL { get; }

        //----- method -----

        /// <summary> 初期化. </summary>
        public virtual void Initialize()
        {
            if (initialized) { return; }

            aesManaged = CreateAesManaged();
            reportForm = new List<IMultipartFormSection>();

            UpdateView();

            sendReportButton.OnClickAsObservable()
                .Subscribe(_ =>
                    {
                        if (sendReportDisposable != null)
                        {
                            PostCancel();
                        }
                        else
                        {
                            sendReportDisposable = Observable.FromCoroutine(() => PostReport())
                                .Subscribe(__ =>
                                {
                                    sendReportDisposable = null;
                                })
                                .AddTo(this);
                        }
                    })
                .AddTo(this);

            initialized = true;
        }

        void OnEnable()
        {
            reportContents = SRTrackLogService.Logs;

            SetReportText(reportContents);

            UpdateView();
        }

        private IEnumerator PostReport()
        {
            yield return CaptureScreenshot();

            yield return new WaitForEndOfFrame();

            UpdateView();

            // 進捗.
            var notifier = new ScheduledNotifier<float>();
            notifier.Subscribe(x => UpdatePostProgress(x));

            reportForm.Clear();

            // 送信内容構築.
            BuildPostContent();

            // 送信.
            var webRequest = UnityWebRequest.Post(PostReportURL, reportForm);

            webRequest.timeout = 30;

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                notifier.Report(operation.progress);

                yield return null;
            }

            // 終了.
            OnReportComplete(webRequest.error);

            reportForm = null;
        }

        private void UpdatePostProgress(float progress)
        {
            progressBarText.text = string.Format("{0}%", progress * 100f);
            progressBar.value = progress;
        }

        private void UpdateView()
        {
            var status = sendReportDisposable == null;

            sendReportButtonText.text = status ? "Send Report" : "Cancel";
            UnityUtility.SetActive(progressBar, !status);

            // 送信中.
            if (status)
            {
                UpdatePostProgress(0f);
            }
        }

        private void OnReportComplete(string errorMessage)
        {
            progressBar.value = 0;

            SRDebug.Instance.ShowDebugPanel(false);

            if (string.IsNullOrEmpty(errorMessage))
            {
                Debug.Log("Bug report submitted successfully.");
            }
            else
            {
                Debug.LogErrorFormat("Error sending bug report." + "\n\n" + errorMessage);
            }

            sendReportDisposable = null;

            UpdateView();
        }

        private void PostCancel()
        {
            if (sendReportDisposable != null)
            {
                sendReportDisposable.Dispose();
                sendReportDisposable = null;
            }

            UpdateView();
        }

        private IEnumerator CaptureScreenshot()
        {
            BugReportScreenshotUtil.ScreenshotData = null;

            SRDebug.Instance.HideDebugPanel();

            yield return BugReportScreenshotUtil.ScreenshotCaptureCo();

            SRDebug.Instance.ShowDebugPanel(false);
        }

        private void SetReportText(LogEntry[] contents)
        {
            var builder = new StringBuilder();

            foreach (var content in contents)
            {
                builder.AppendLine(content.Message).AppendLine();
            }

            var reportText = builder.ToString();

            // Unityは15000文字くらいまでしか表示対応していない.
            if (14000 < reportText.Length)
            {
                reportText = reportText.SafeSubstring(0, 14000) + "<message truncated>";
            }

            reportContentText.text = reportText;
        }

        private string GetReportTextPostData()
        {
            if (reportContents.IsEmpty())
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var content in reportContents)
            {
                builder.AppendFormat("Type: {0}", Enum.GetName(typeof(LogType), content.LogType)).AppendLine();
                builder.AppendFormat("Message: {0}", content.Message).AppendLine();
                builder.AppendFormat("StackTrace:\n{0}", content.StackTrace).AppendLine();
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private void BuildPostContent()
        {
            reportForm = new List<IMultipartFormSection>();

            const uint mega = 1024 * 1024;

            var lastLog = reportContents.LastOrDefault();

            //------ Screenshot ------

            var bytes = BugReportScreenshotUtil.ScreenshotData;

            var screenshotBase64 = Convert.ToBase64String(bytes);

            // スクリーンショット情報破棄.
            BugReportScreenshotUtil.ScreenshotData = null;

            //------ ReportContent ------

            AddReportContent("logType", (lastLog != null ? lastLog.LogType : LogType.Log).ToString());
            AddReportContent("log", GetReportTextPostData());
            AddReportContent("comment", commentInputField.text);
            AddReportContent("time", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            AddReportContent("operatingSystem", SystemInfo.operatingSystem);
            AddReportContent("deviceModel", SystemInfo.deviceModel);
            AddReportContent("systemMemorySize", (SystemInfo.systemMemorySize * mega).ToString());
            AddReportContent("useMemorySize", GC.GetTotalMemory(false).ToString());
            AddReportContent("screenShotBase64", screenshotBase64);

            // 拡張情報を追加.
            SetExtendContents();
        }

        /// <summary> 送信情報に追加 </summary>
        protected void AddReportContent(string key, string value)
        {
            if (reportForm == null) { return; }

            if (!string.IsNullOrEmpty(value))
            {
                value = aesManaged != null ? value.Encrypt(aesManaged) : value;
            }

            reportForm.Add(new MultipartFormDataSection(key, value));
        }

        /// <summary> AESクラス生成 </summary>
        protected virtual AesManaged CreateAesManaged() { return null; }

        /// <summary> 拡張情報を追加 </summary>
        protected virtual void SetExtendContents() { }

        #endif
    }
}
