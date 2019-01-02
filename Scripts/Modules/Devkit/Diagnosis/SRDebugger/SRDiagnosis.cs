﻿﻿
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;
using Modules.Devkit.Log;

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
                // 一旦アクティブにならないとログ収集しないので一瞬アクティブ化.
                SRDebug.Instance.ShowDebugPanel();
                SRDebug.Instance.HideDebugPanel();

                UnityUtility.SetActive(blockCollider, SRDebug.Instance.IsDebugPanelVisible);

                SRDebug.Instance.PanelVisibilityChanged += x => { UnityUtility.SetActive(blockCollider, x); };

                button.OnClickAsObservable()
                    .Subscribe(_ => OnClick())
                    .AddTo(this);

                ApplicationLogHandler.Instance.OnLogReceiveAsObservable()
                    .Subscribe(x => OnLogReceive(x))
                    .AddTo(this);

                SRTrackLogService.Initialize();

                initialized = true;
            }
        }

        private void OnClick()
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
