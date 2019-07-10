
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;
using Modules.GameText.Components;

namespace Modules.Devkit.CleanComponent
{
    #if !UNITY_2018_3_OR_NEWER

    public sealed class PrefabTextComponentCleaner : TextComponentCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            PrefabApplyHook.OnApplyPrefabAsObservable().Subscribe(x => OnApplyPrefab(x));
        }

        private static void OnApplyPrefab(GameObject prefab)
        {
            if (prefab == null) { return; }

            if (!Prefs.autoClean) { return; }

            var gameObjects = prefab.DescendantsAndSelf().ToArray();

            if (!CheckExecute(gameObjects)) { return; }

            foreach (var gameObject in gameObjects)
            {
                ModifyTextComponent(gameObject);
            }
        }
    }

    #endif
}
