
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        private CancellationTokenSource cancelSource = null;

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
                .Subscribe(async _ =>
                    {
                        if (cancelSource != null)
                        {
                            PostCancel();
                        }
                        else
                        {
							await SendReport();
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

        private async UniTask SendReport()
        {
            var sendReportManager = SendReportManager.Instance;

            UpdatePostProgress(0f);

            await CaptureScreenShot();

			// 進捗.
            var notifier = new ScheduledNotifier<float>();

            notifier.Subscribe(x => UpdatePostProgress(x));

			try
			{
				cancelSource = new CancellationTokenSource();

				await sendReportManager.Send(ReportTitle, notifier).AttachExternalCancellation(cancelSource.Token);

				cancelSource = null;
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

        private void UpdatePostProgress(float progress)
        {
            progressBarText.text = string.Format("{0}%", progress * 100f);
            progressBar.value = progress;
        }

        private void UpdateView()
        {
            var status = cancelSource == null;

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

			RefreshInputField(titleInputField);

            using (new DisableStackTraceScope())
            {
                if (string.IsNullOrEmpty(errorMessage))
                {
                    OnRequestRefreshInputText();
                    
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
			if (cancelSource != null)
			{
				cancelSource.Cancel();
			}

            UpdateView();
        }

        private async UniTask CaptureScreenShot()
        {
			var srDebug = SRDebug.Instance;
            var sendReportManager = SendReportManager.Instance;
            
			srDebug.HideDebugPanel();

			await UniTask.NextFrame();

            sendReportManager.CaptureScreenShot();

			srDebug.ShowDebugPanel(false);

			await UniTask.NextFrame();
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

        #endif
    }
}
