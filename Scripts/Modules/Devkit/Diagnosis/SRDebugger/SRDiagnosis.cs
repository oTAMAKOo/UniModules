
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;
using Modules.Devkit.Diagnosis.LogTracker;
using Modules.Devkit.LogHandler;

#if ENABLE_SRDEBUGGER

using SRDebugger;

#endif

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public sealed class SRDiagnosis : MonoBehaviour
    {
        #if ENABLE_SRDEBUGGER

        //----- params -----

        private static readonly LogType[] LogPriority = new LogType[]
        {
            LogType.Log,
            LogType.Warning,
            LogType.Error,
            LogType.Exception,
        };

        //----- field -----

        [SerializeField]
        private Button button = null;
        [SerializeField]
        private Image background = null;
        [SerializeField]
        private Color defaultColor = Color.white;
        [SerializeField]
        private Color warningColor = Color.yellow;
        [SerializeField]
        private Color errorColor = Color.red;
        [SerializeField]
        private Color exceptionColor = Color.magenta;

        private GameObject touchBlock = null;

        private LogType? currentLogType = null;

        private float lastShowLogTime = 0f;

        private bool? isEnable = null;

        private bool initialized = false;

        //----- property -----

        public bool IsEnable
        {
            get { return isEnable.HasValue ? isEnable.Value : UnityEngine.Debug.isDebugBuild; }
            set { isEnable = value; }
        }

        //----- method -----

        public void Initialize()
        {
            if (!initialized && IsEnable)
            {
                SRDebug.Init();

                var srDebug = SRDebug.Instance;
                var logTracker = UnityLogTracker.Instance;
                var applicationLogHandler = ApplicationLogHandler.Instance;

                SetTouchBlock(touchBlock);

                VisibilityChangedDelegate onPanelVisibilityChanged = visible =>
                {
                    if (visible)
                    {
                        background.color = defaultColor;
                        currentLogType = null;
                        lastShowLogTime = Time.realtimeSinceStartup;
                    }

                    UnityUtility.SetActive(touchBlock, visible);
                };

                srDebug.PanelVisibilityChanged += onPanelVisibilityChanged;

                button.OnClickAsObservable()
                    .Subscribe(_ => srDebug.ShowDebugPanel())
                    .AddTo(this);

                applicationLogHandler.OnReceivedThreadedAllAsObservable()
                    .ObserveOnMainThread()
                    .Subscribe(x => OnLogReceive(x))
                    .AddTo(this);

                logTracker.Initialize();

                background.color = defaultColor;

                initialized = true;
            }
        }

        public void SetTouchBlock(GameObject touchBlock)
        {
            this.touchBlock = touchBlock;

            var srDebug = SRDebug.Instance;

            UnityUtility.SetActive(touchBlock, srDebug.IsDebugPanelVisible);
        }

        private void OnLogReceive(ApplicationLogHandler.LogInfo logInfo)
        {
            var srDebug = SRDebug.Instance;

            if (srDebug == null) { return; }

            var changeColor = true;

            var ignoreWarnings = new string[]
            {
                "SpriteAtlasManager.atlasRequested wasn't listened to while",
            };

            foreach (var ignore in ignoreWarnings)
            {
                if (logInfo.Condition.StartsWith(ignore)) { return; }
            }
            
            if(currentLogType.HasValue)
            {
                var priority = LogPriority.IndexOf(x => x == logInfo.Type);
                var current = LogPriority.IndexOf(x => x == currentLogType.Value);

                changeColor = current < priority;
            }

            if (changeColor)
            {
                var color = defaultColor;

                switch (logInfo.Type)
                {
                    case LogType.Warning:
                        color = warningColor;
                        break;

                    case LogType.Error:
                        color = errorColor;
                        break;

                    case LogType.Exception:
                        color = exceptionColor;
                        break;
                }

                if (!srDebug.IsDebugPanelVisible)
                {
                    background.color = lastShowLogTime < Time.realtimeSinceStartup ? color : defaultColor;
                }

                currentLogType = logInfo.Type;
            }            
        }
    }
}
