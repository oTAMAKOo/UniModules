
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;
using Modules.Devkit.Log;

#if ENABLE_SRDEBUGGER

using SRDebugger;

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public class SRDiagnosis : MonoBehaviour
    {
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
        private GameObject blockCollider = null;
        [SerializeField]
        private Color defaultColor = Color.white;
        [SerializeField]
        private Color warningColor = Color.yellow;
        [SerializeField]
        private Color errorColor = Color.red;
        [SerializeField]
        private Color exceptionColor = Color.magenta;

        private LogType? currentLogType = null;

        private float lastShowLogTime = 0f;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        private bool IsEnable()
        {
            return UnityEngine.Debug.isDebugBuild;
        }

        public void Initialize()
        {
            if (!initialized && IsEnable())
            {
                SRDebug.Init();

                UnityUtility.SetActive(blockCollider, SRDebug.Instance.IsDebugPanelVisible);

                VisibilityChangedDelegate onPanelVisibilityChanged = visible =>
                {
                    if (visible)
                    {
                        ResetLogTrack();
                    }

                    UnityUtility.SetActive(blockCollider, visible);
                };

                SRDebug.Instance.PanelVisibilityChanged += onPanelVisibilityChanged;

                button.OnClickAsObservable()
                    .Subscribe(_ => ResetLogTrack())
                    .AddTo(this);

                ApplicationLogHandler.Instance.OnLogReceiveAsObservable()
                    .Subscribe(x => OnLogReceive(x))
                    .AddTo(this);

                SRTrackLogService.Initialize();

                UnityUtility.SetActive(blockCollider, false);

                background.color = defaultColor;

                initialized = true;
            }
        }

        private void ResetLogTrack()
        {
            if (!SRDebug.Instance.IsDebugPanelVisible)
            {
                SRDebug.Instance.ShowDebugPanel();
                background.color = defaultColor;
                currentLogType = null;
                lastShowLogTime = Time.realtimeSinceStartup;
            }
        }

        private void OnLogReceive(ApplicationLogHandler.LogInfo logInfo)
        {
            var changeColor = true;

            //----- TODO: Unity2018.2以降で無駄な警告が出るので、修正されるまで除外. -----

            var ignoreUnity2018_2Warnings = new string[]
            {
                "Your multi-scene setup may be improved by tending to the following issues",
                "Your current multi-scene setup has inconsistent Lighting settings",
            };

            foreach (var ignore in ignoreUnity2018_2Warnings)
            {
                if (logInfo.Condition.StartsWith(ignore)) { return; }
            }
            
            //-----------------------------------------------------------------------------------

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

                if (!SRDebug.Instance.IsDebugPanelVisible)
                {
                    background.color = lastShowLogTime < Time.realtimeSinceStartup ? color : defaultColor;
                }

                currentLogType = logInfo.Type;
            }            
        }
    } 
}

#endif
