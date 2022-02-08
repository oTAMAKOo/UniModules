
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using UniRx;
using Extensions;
using Modules.Devkit.Diagnosis.SendReport;

#if ENABLE_SRDEBUGGER

using SRDebugger;

#endif

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public abstract class SendReportSheetController : MonoBehaviour
    {
        #if ENABLE_SRDEBUGGER

        //----- params -----

        //----- field -----

        [SerializeField]
        private InputField titleInputField = null;
        [SerializeField]
        private Button sendReportButton = null;
        [SerializeField]
        private Text sendReportButtonText = null;
        [SerializeField]
        private Slider progressBar = null;
        [SerializeField]
        private Text progressBarText = null;

        private IDisposable sendReportDisposable = null;

        protected bool initialized = false;

        //----- property -----

        public string ReportTitle { get { return titleInputField.text; } }

        //----- method -----

        /// <summary> 初期化. </summary>
        public virtual void Initialize()
        {
            if (initialized) { return; }

            var srDebug = SRDebug.Instance;

            var sendReportManager = SendReportManager.Instance;

            VisibilityChangedDelegate onPanelVisibilityChanged = visible =>
            {
                if (visible && gameObject.activeInHierarchy)
                {
                    UpdateView();
                }
            };

            srDebug.PanelVisibilityChanged += onPanelVisibilityChanged;

            UpdateView();

            sendReportManager.OnRequestReportAsObservable()
                .Subscribe(_ =>
                   {
                       AddReportTitle();
                       UpdateView();
                   })
                .AddTo(this);

            sendReportManager.OnReportCompleteAsObservable()
                .Subscribe(x => OnReportComplete(x))
                .AddTo(this);

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
                                .Subscribe(__ => sendReportDisposable = null)
                                .AddTo(this);
                        }
                    })
                .AddTo(this);

            initialized = true;
        }

        void OnEnable()
        {
            UpdateView();

            Observable.NextFrame()
                .TakeUntilDisable(this)
                .Subscribe(_ => OnRequestRefreshInputText())
                .AddTo(this);

            Observable.EveryUpdate()
                .TakeUntilDisable(this)
                .Subscribe(_ => UnityUtility.SetActive(sendReportButton, IsSendReportButtonEnable()))
                .AddTo(this);
        }

        private void AddReportTitle()
        {
            var sendReportManager = SendReportManager.Instance;

            var reportTitle = titleInputField.text;

            if (string.IsNullOrEmpty(reportTitle)) { return; }

            sendReportManager.AddReportContent("Title", titleInputField.text);
        }

        private IEnumerator SendReport()
        {
            var sendReportManager = SendReportManager.Instance;

            UpdatePostProgress(0f);

            yield return CaptureScreenShot();

            yield return new WaitForEndOfFrame();

            // 進捗.
            var notifier = new ScheduledNotifier<float>();

            notifier.Subscribe(x => UpdatePostProgress(x));

            var sendYield = sendReportManager.Send(notifier).ToYieldInstruction(false);

            while (!sendYield.IsDone)
            {
                yield return null;
            }

            if (sendYield.HasError)
            {
                Debug.LogException(sendYield.Error);
            }
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

            if (sendReportDisposable != null)
            {
                sendReportDisposable.Dispose();
                sendReportDisposable = null;
            }

            RefreshInputField(titleInputField);

            using (new DisableStackTraceScope())
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    OnRequestRefreshInputText();

                    OnSubmitSuccess();

                    Debug.Log("Bug report submitted successfully.");
                }
                else
                {
                    Debug.LogErrorFormat("Error sending bug report." + "\n\n" + errorMessage);
                }
            }

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
            var sendReportManager = SendReportManager.Instance;
            
            SRDebug.Instance.HideDebugPanel();

            yield return sendReportManager.CaptureScreenShot();

            SRDebug.Instance.ShowDebugPanel(false);
        }

        /// <summary> InputFieldをリフレッシュ </summary>
        protected void RefreshInputField(InputField inputField)
        {
            inputField.text = string.Empty;
            inputField.MoveTextEnd(false);
            inputField.OnDeselect(new BaseEventData(EventSystem.current));
        }

        /// <summary> 以前の入力をリフレッシュ </summary>
        protected virtual void OnRequestRefreshInputText() { }

        /// <summary> レポートボタンが有効か </summary>
        protected virtual bool IsSendReportButtonEnable() { return true; }

        /// <summary> 送信成功時関数 </summary>
        protected virtual void OnSubmitSuccess() { }

        #endif
    }
}
