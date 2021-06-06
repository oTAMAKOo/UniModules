
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UniRx;
using Extensions;

#if ENABLE_SRDEBUGGER

using SRDebugger;
using SRDebugger.Internal;

#endif

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public enum PostDataType
    {
        Foam,
        Json,
    }

    public abstract class SendReportSheetController : MonoBehaviour
    {
        #if ENABLE_SRDEBUGGER

        //----- params -----

        public sealed class LogContainer
        {
            public LogEntry[] contents = null;
        }

        //----- field -----

        [SerializeField]
        private Text reportContentText = null;
        [SerializeField]
        private Button sendReportButton = null;
        [SerializeField]
        private Text sendReportButtonText = null;
        [SerializeField]
        private Slider progressBar = null;
        [SerializeField]
        private Text progressBarText = null;

        protected Dictionary<string, string> reportData = null;

        private IDisposable sendReportDisposable = null;

        private AesCryptKey aesCryptKey = null;

        protected bool initialized = false;

        //----- property -----

        public PostDataType PostDataType { get; protected set; }

        //----- method -----

        /// <summary> 初期化. </summary>
        public virtual void Initialize()
        {
            if (initialized) { return; }

            reportData = new Dictionary<string, string>();

            aesCryptKey = CreateCryptKey();

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
                            sendReportDisposable = Observable.FromCoroutine(() => SendReport())
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
            UpdateContents();

            Observable.NextFrame()
                .TakeUntilDisable(this)
                .Subscribe(_ => OnRequestRefreshInputText())
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

        private IEnumerator SendReport()
        {
            yield return CaptureScreenShot();

            yield return new WaitForEndOfFrame();

            UpdateView();

            // 進捗.
            var notifier = new ScheduledNotifier<float>();
            notifier.Subscribe(x => UpdatePostProgress(x));

            reportData.Clear();

            // 送信内容構築.
            BuildPostContent();
            
            // 送信.

            var postReportYield = Observable.FromMicroCoroutine<string>(observer => PostReport(observer, notifier)).ToYieldInstruction();

            while (!postReportYield.IsDone)
            {
                yield return null;
            }

            // 終了.
            OnReportComplete(postReportYield.Result);
            
            reportData.Clear();
        }

        protected virtual IEnumerator PostReport(IObserver<string> observer, IProgress<float> progress)
        {
            var url = GetReportUrl();

            UnityWebRequest webRequest = null;

            switch (PostDataType)
            {
                case PostDataType.Foam:
                    webRequest = UnityWebRequest.Post(url, CreateReportFormSections());
                    break;

                case PostDataType.Json:
                    webRequest = UnityWebRequest.Post(url, reportData.ToJson());
                    break;
            }

            webRequest.timeout = 30;

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                progress.Report(operation.progress);

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
                OnRequestRefreshInputText();

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

        private IEnumerator CaptureScreenShot()
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
                builder.AppendLine(log.message).AppendLine();
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
            var logs = SRTrackLogService.Logs.ToArray();

            if (logs.IsEmpty()) { return string.Empty; }

            var container = new LogContainer() { contents = logs };
            
            return JsonUtility.ToJson(container);
        }

        private void BuildPostContent()
        {
            reportData = new Dictionary<string, string>();

            const uint mega = 1024 * 1024;

            //------ ScreenShot ------

            var bytes = BugReportScreenshotUtil.ScreenshotData;

            var screenShotBase64 = Convert.ToBase64String(bytes);

            // スクリーンショット情報破棄.
            BugReportScreenshotUtil.ScreenshotData = null;

            //------ ReportContent ------

            AddReportContent("Time", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            AddReportContent("OperatingSystem", SystemInfo.operatingSystem);
            AddReportContent("DeviceModel", SystemInfo.deviceModel);
            AddReportContent("SystemMemorySize", (SystemInfo.systemMemorySize * mega).ToString());
            AddReportContent("UseMemorySize", GC.GetTotalMemory(false).ToString());
            AddReportContent("Log", GetReportTextPostData());
            AddReportContent("ScreenShotBase64", screenShotBase64);

            // 拡張情報を追加.

            SetExtendContents();
        }

        /// <summary> 送信情報に追加 </summary>
        protected void AddReportContent(string key, string value)
        {
            if (reportData == null) { return; }

            if (string.IsNullOrEmpty(value))
            {
                value = "---";
            }

            value = aesCryptKey != null ? value.Encrypt(aesCryptKey) : value;

            reportData.Add(key, value);
        }

        protected List<IMultipartFormSection> CreateReportFormSections()
        {
            var reportForm = new List<IMultipartFormSection>();

            foreach (var item in reportData)
            {
                reportForm.Add(new MultipartFormDataSection(item.Key, item.Value));
            }

            return reportForm;
        }

        protected string CreateReportJson()
        {
            return reportData.ToJson();
        }

        /// <summary> InputFieldをリフレッシュ </summary>
        protected void RefreshInputField(InputField inputField)
        {
            inputField.text = string.Empty;
            inputField.MoveTextEnd(false);
            inputField.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <summary> 暗号化キー生成 </summary>
        protected virtual AesCryptKey CreateCryptKey() { return null; }

        /// <summary> 以前の入力をリフレッシュ </summary>
        protected virtual void OnRequestRefreshInputText() { }

        /// <summary> レポートボタンが有効か </summary>
        protected virtual bool IsSendReportButtonEnable() { return true; }

        /// <summary> 拡張情報を追加 </summary>
        protected virtual void SetExtendContents() { }

        /// <summary> 送信先URL </summary>
        protected abstract string GetReportUrl();

        #endif
    }
}
