﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Diagnosis.SRDebugger;

namespace Modules.Devkit.Diagnosis
{
    public class Diagnosis : MonoBehaviour
    {
        //----- params -----

        //----- field -----


        [SerializeField]
        private GameObject touchBlockObject = null;
        [SerializeField]
        private FpsStats fpsStats = null;
        [SerializeField]
        private SRDiagnosis srDiagnosis = null;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            UnityUtility.SetActive(touchBlockObject, false);

            fpsStats.Initialize();

            #if ENABLE_SRDEBUGGER

            srDiagnosis.Initialize();

            #endif
        }
    }
}
