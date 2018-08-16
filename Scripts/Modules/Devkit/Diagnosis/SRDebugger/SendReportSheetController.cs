
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using UniRx;
using Extensions;
using SRDebugger.Internal;
using System.Text;

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public abstract class SendReportSheetController : MonoBehaviour
    {
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

        private LogEntry[] reportContents = null;

        private IDisposable sendReportDisposable = null;

        //----- property -----

        public abstract string PostReportURL
        {
            get;
        }
        public abstract string Uid
        {
            get;
        }

        //----- method -----

        public void Initialize()
        {
            UpdateView();

            sendReportButton.OnClickAsObservable().Subscribe(
                _ =>
                {
                    if (sendReportDisposable != null)
                    {
                        PostCancel();
                    }
                    else
                    {
                        sendReportDisposable = Observable.FromCoroutine(() => PostReport())
                            .Subscribe(
                                __ =>
                                {
                                    sendReportDisposable = null;

                                })
                            .AddTo(this);
                    }
                }).AddTo(this);
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

            // 送信.
            Exception ex = null;

            var url = PostReportURL;
            var content = CreatePostContent();

            yield return ObservableWWW.Post(url, content, notifier)
                .Timeout(TimeSpan.FromSeconds(30))
                .StartAsCoroutine(error => ex = error, CancellationToken.Empty);

            // 終了.
            OnReportComplete(ex == null ? string.Empty : ex.ToString());
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

        private static string GetReportText(LogEntry[] contents)
        {
            if (contents.IsEmpty())
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (var content in contents)
            {
                builder.AppendFormat("Type: {0}", Enum.GetName(typeof(LogType), content.LogType)).AppendLine();
                builder.AppendFormat("Message: {0}", content.Message).AppendLine();
                builder.AppendFormat("StackTrace:\n{0}", content.StackTrace).AppendLine();
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private WWWForm CreatePostContent()
        {
            const uint mega = 1024 * 1024;

            var lastLog = reportContents.LastOrDefault();

            var form = new WWWForm();

            form.AddField("uid", Uid);
            form.AddField("logType", (lastLog != null ? lastLog.LogType : LogType.Log).ToString());
            form.AddField("log", GetReportText(reportContents));
            form.AddField("comment", commentInputField.text);
            form.AddField("time", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            form.AddField("operatingSystem", SystemInfo.operatingSystem);
            form.AddField("deviceModel", SystemInfo.deviceModel);
            form.AddField("systemMemorySize", (SystemInfo.systemMemorySize * mega).ToString());
            form.AddField("useMemorySize", (int)GC.GetTotalMemory(false));
            form.AddField("screenShotBase64", Convert.ToBase64String(BugReportScreenshotUtil.ScreenshotData));

            // スクリーンショット情報破棄.
            BugReportScreenshotUtil.ScreenshotData = null;

            return form;
        }
    }
}