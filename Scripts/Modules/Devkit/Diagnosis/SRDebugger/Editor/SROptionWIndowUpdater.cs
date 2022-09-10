#if ENABLE_SRDEBUGGER

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UniRx;
using Extensions;
using SRDebugger.Editor;
using SRDebugger.Internal;
using SRDebugger.Services;
using SRF.Service;

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public static class SROptionWindowUpdater
    {
        //----- params -----

        //----- field -----

        private static SROptionsWindow[] targetWindows = null;

        private static FieldInfo optionsFieldInfo = null;
        private static MethodInfo populateMethodInfo = null;

        //----- property -----

        //----- method -----

        [InitializeOnEnterPlayMode]
        private static void InitializeOnEnterPlayMode()
        {
            optionsFieldInfo = Reflection.GetFieldInfo(typeof(SROptionsWindow), "_options", BindingFlags.NonPublic | BindingFlags.Instance);
            populateMethodInfo = Reflection.GetMethodInfo(typeof(SROptionsWindow), "Populate", BindingFlags.NonPublic | BindingFlags.Instance);
           
            Observable.Interval(TimeSpan.FromSeconds(3)).Subscribe(_ => SyncContents());
        }

        private static void SyncContents()
        {
            if (!SRServiceManager.HasService<IDebugService>()){ return; }

            targetWindows = Resources.FindObjectsOfTypeAll<SROptionsWindow>();

            if (targetWindows.IsEmpty()){ return; }

            foreach (var targetWindow in targetWindows)
            {
                var options = new Dictionary<string, List<OptionDefinition>>();

                if (optionsFieldInfo != null)
                {
                    options = optionsFieldInfo.GetValue(targetWindow) as Dictionary<string, List<OptionDefinition>>;
                }

                if (options == null){ continue; }

                if (options.IsEmpty() && Service.Options.Options.Any())
                {
                    populateMethodInfo.Invoke(targetWindow, new object[]{});
                }
            }
        }
    }
}

#endif
