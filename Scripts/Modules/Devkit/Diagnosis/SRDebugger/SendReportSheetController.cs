
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;
using UniRx;
using Extensions;

#if ENABLE_SRDEBUGGER

using SRDebugger;
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
        private InputField titleInputField = null;
        [SerializeField]
        private InputField commentInputField = null;
        [SerializeField]
        private Slider progressBar = null;
        [SerializeField]
        private Text progressBarText = null;

        private List<IMultipartFormSection> reportForm = null;
        
        private IDisposable sendReportDisposable = null;

        private AesManaged aesManaged = null;

        protected bool initialized = false;

        //----- property -----

        //----- method -----

        /// <summary> 初期化. </summary>
        public virtual void Initialize()
        {
            if (initialized) { return; }

            aesManaged = CreateAesManaged();
            reportForm = new List<IMultipartFormSection>();

            var srDebug = SRDebug.Instance;

            VisibilityChangedDelegate onPanelVisibilityChanged = visible =>
            {
                if (visible && gameObject.activeInHierarchy)
                {
                    UpdateContents();
                }
            };

            srDebug.PanelVisibilityChanged += onPanelVisibilityChanged;

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

            titleInputField.text = string.Empty;
            commentInputField.text = string.Empty;

            initialized = true;
        }

        void OnEnable()
        {
            UpdateContents();

            Observable.NextFrame()
                .TakeUntilDisable(this)
                .Subscribe(_ => RefreshInputField())
                .AddTo(this);

            Observable.EveryUpdate()
                .TakeUntilDisable(this)
                .Subscribe(_ => UnityUtility.SetActive(sendReportButton, IsSendReportButtonEnable()))
                .AddTo(this);
        }

        private void UpdateContents()
        {
            SetReportText();

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

            var url = GetReportUrl();

            var webRequest = UnityWebRequest.Post(url, reportForm);

            webRequest.timeout = 30;

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                notifier.Report(operation.progress);

                yield return null;
            }

            var errorMessage = string.Empty;

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                errorMessage = string.Format("[{0}]{1}", webRequest.responseCode, webRequest.error);
            }

            // 終了.
            OnReportComplete(errorMessage);

            reportForm.Clear();
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
                RefreshInputField();

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

        private void SetReportText()
        {
            var logs = SRTrackLogService.Logs;

            var builder = new StringBuilder();

            foreach (var log in logs)
            {
                builder.AppendLine(log.Message).AppendLine();
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
            var logs = SRTrackLogService.Logs;

            if (logs.IsEmpty()) { return string.Empty; }

            var builder = new StringBuilder();

            foreach (var log in logs)
            {
                builder.AppendFormat("Type: {0}", Enum.GetName(typeof(LogType), log.LogType)).AppendLine();
                builder.AppendFormat("Message: {0}", log.Message).AppendLine();

                if (!string.IsNullOrEmpty(log.StackTrace))
                {
                    builder.AppendFormat("StackTrace:\n{0}", log.StackTrace).AppendLine();
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private void BuildPostContent()
        {
            reportForm = new List<IMultipartFormSection>();

            const uint mega = 1024 * 1024;

            //------ ScreenShot ------

            var bytes = BugReportScreenshotUtil.ScreenshotData;

            var screenshotBase64 = Convert.ToBase64String(bytes);

            // スクリーンショット情報破棄.
            BugReportScreenshotUtil.ScreenshotData = null;

            //------ ReportContent ------

            AddReportContent("Time", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            AddReportContent("OperatingSystem", SystemInfo.operatingSystem);
            AddReportContent("DeviceModel", SystemInfo.deviceModel);
            AddReportContent("SystemMemorySize", (SystemInfo.systemMemorySize * mega).ToString());
            AddReportContent("UseMemorySize", GC.GetTotalMemory(false).ToString());
            AddReportContent("Log", GetReportTextPostData());
            AddReportContent("ScreenShotBase64", screenshotBase64);

            // ユーザー入力情報.

            if (!string.IsNullOrEmpty(titleInputField.text))
            {
                AddReportContent("Title", titleInputField.text);
            }

            if (!string.IsNullOrEmpty(commentInputField.text))
            {
                AddReportContent("Comment", commentInputField.text);
            }

            // 拡張情報を追加.

            SetExtendContents();
        }

        /// <summary> 送信情報に追加 </summary>
        protected void AddReportContent(string key, string value)
        {
            if (reportForm == null) { return; }

            if (string.IsNullOrEmpty(value))
            {
                value = "---";
            }

            value = aesManaged != null ? value.Encrypt(aesManaged) : value;

            reportForm.Add(new MultipartFormDataSection(key, value));
        }

        /// <summary> InputFieldを初期化 </summary>
        protected virtual void RefreshInputField()
        {
            titleInputField.text = string.Empty;
            titleInputField.MoveTextEnd(false);
            titleInputField.OnDeselect(new BaseEventData(EventSystem.current));

            commentInputField.text = string.Empty;
            commentInputField.MoveTextEnd(false);
            commentInputField.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <summary> レポートボタンが有効か </summary>
        protected virtual bool IsSendReportButtonEnable()
        {
            return !string.IsNullOrEmpty(titleInputField.text);
        }

        /// <summary> AESクラス生成 </summary>
        protected virtual AesManaged CreateAesManaged() { return null; }

        /// <summary> 拡張情報を追加 </summary>
        protected virtual void SetExtendContents() { }

        /// <summary> 送信先URL </summary>
        protected abstract string GetReportUrl();

        #endif
    }
}
