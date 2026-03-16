#if ENABLE_SRDEBUGGER

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UniRx;
using Extensions;
using SRDebugger;
using SRDebugger.Internal;
using SRDebugger.Services;
using SRF.Service;

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public static class SROptionWindowUpdater
    {
        //----- params -----

        //----- field -----

        private static Type optionsWindowType = null;

        private static FieldInfo optionsFieldInfo = null;
        private static MethodInfo populateMethodInfo = null;

        //----- property -----

        //----- method -----

        [InitializeOnEnterPlayMode]
        private static void InitializeOnEnterPlayMode()
        {
            optionsWindowType = Assembly.Load("StompyRobot.SRDebugger.Editor")
                .GetTypes()
                .FirstOrDefault(t => t.Name == "SROptionsWindow");

            if (optionsWindowType == null){ return; }

            optionsFieldInfo = Reflection.GetFieldInfo(optionsWindowType, "_options", BindingFlags.NonPublic | BindingFlags.Instance);
            populateMethodInfo = Reflection.GetMethodInfo(optionsWindowType, "Populate", BindingFlags.NonPublic | BindingFlags.Instance);

            Observable.Interval(TimeSpan.FromSeconds(3)).Subscribe(_ => SyncContents());
        }

        private static void SyncContents()
        {
            if (optionsWindowType == null){ return; }

            if (!SRServiceManager.HasService<IDebugService>()){ return; }

            var targetWindows = Resources.FindObjectsOfTypeAll(optionsWindowType);

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
