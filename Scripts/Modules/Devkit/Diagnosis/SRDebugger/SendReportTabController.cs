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

namespace Modules.Devkit.Diagnosis.SRDebugger
{
	public class SendReportTabController : SRMonoBehaviourEx, IEnableTab
    {
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
    }
}

#endif
