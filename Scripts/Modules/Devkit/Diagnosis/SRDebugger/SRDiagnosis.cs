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
        private Color errorColor = Color.red;

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
                    .Subscribe(_ =>
                        {
                            if (!SRDebug.Instance.IsDebugPanelVisible)
                            {
                                SRDebug.Instance.ShowDebugPanel();
                                background.color = defaultColor;
                                lastShowLogTime = Time.realtimeSinceStartup;
                            }
                        })
                    .AddTo(this);

                ApplicationLogHandler.Instance.OnLogReceiveAsObservable()
                    .Subscribe(x =>
                        {
                            if (x.Type == LogType.Error || x.Type == LogType.Exception)
                            {
                                if (!SRDebug.Instance.IsDebugPanelVisible)
                                {
                                    background.color = lastShowLogTime < Time.realtimeSinceStartup ? errorColor : defaultColor;
                                }
                            }
                        })
                    .AddTo(this);

                SRTrackLogService.Initialize();

                initialized = true;
            }
        }
    } 
}