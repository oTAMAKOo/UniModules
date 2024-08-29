
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using UniRx;
using System.Linq;
using Extensions;
using Modules.Devkit.Diagnosis.LogTracker;
using Modules.Devkit.LogHandler;
using Modules.Resolution;

#if ENABLE_SRDEBUGGER

using SRDebugger.Services.Implementation;
using SRDebugger.Internal;

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
            get
            {
                #if FORCE_SRDIAGNOSIS_ENABLE

                return true;

                #else

                return isEnable.HasValue ? isEnable.Value : UnityEngine.Debug.isDebugBuild;

                #endif
            }

            set { isEnable = value; }
        }

        public string[] IgnoreWarnings { get; set; }

        public string[] IgnoreErrors { get; set; }

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

                void OnPanelVisibilityChanged(bool visible)
                {
                    if (visible)
                    {
                        background.color = defaultColor;
                        currentLogType = null;
                        lastShowLogTime = Time.realtimeSinceStartup;
                    }

                    if (visible)
                    {
                        var debugPanelService = Service.Panel as DebugPanelServiceImpl;

                        if (debugPanelService != null)
                        {
                            var root = debugPanelService.RootObject;

                            if (root != null && root.Canvas != null)
                            {
                                var srContent =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           root.Canvas.gameObject.Children().FirstOrDefault(x => x.name == "SR_Content");
                                UnityUtility.GetOrAddComponent<SafeAreaAdjuster>(srContent);
                            }
                        }
                    }

                    UnityUtility.SetActive(touchBlock, visible);
                }

                srDebug.IsTriggerEnabled = !button.gameObject.activeInHierarchy;
                srDebug.PanelVisibilityChanged += OnPanelVisibilityChanged;

                button.OnClickAsObservable()
                    .Subscribe(_ => srDebug.ShowDebugPanel())
                    .AddTo(this);

                applicationLogHandler.OnReceivedThreadedAllAsObservable()
                    .ObserveOnMainThread()
                    .Subscribe(x => OnLogReceive(x))
                    .AddTo(this);

                logTracker.Initialize();

                background.color = defaultColor;

                IgnoreWarnings = new string[0];
                IgnoreErrors = new string[0];

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

            if (logInfo.Type == LogType.Warning)
            {
                foreach (var ignore in IgnoreWarnings)
                {
                    if (logInfo.Condition.StartsWith(ignore)) { return; }
                }
            }

            if (logInfo.Type == LogType.Error)
            {
                foreach (var ignore in IgnoreErrors)
                {
                    if (logInfo.Condition.StartsWith(ignore)) { return; }
                }
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

                #endif
            }
}
