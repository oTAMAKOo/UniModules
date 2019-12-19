﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

#if ENABLE_SRDEBUGGER

using SRDebugger.UI.Other;
using SRF;

#endif

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    #if !ENABLE_SRDEBUGGER

    public interface IEnableTab
    {
        
    }

    public class SRMonoBehaviourEx : MonoBehaviour
    {
        protected virtual void Start() { }
    }

    #endif
    
    public sealed class SendReportTabController : SRMonoBehaviourEx, IEnableTab
    {
        #if ENABLE_SRDEBUGGER

        //----- params -----

        //----- field -----

        [SerializeField]
        public GameObject sendReportSheetPrefab = null;
        [SerializeField]
        public GameObject container = null;

        private SendReportSheetController sendReportSheet = null;

        //----- property -----

        public bool IsEnabled { get { return true; } }

        //----- method -----

        protected override void Start()
        {
            base.Start();

            sendReportSheet = UnityUtility.Instantiate<SendReportSheetController>(container, sendReportSheetPrefab, false);
            UnityUtility.SetLayer(gameObject, sendReportSheet.gameObject, true);

            sendReportSheet.Initialize();
        }

        #endif
    }    
}
