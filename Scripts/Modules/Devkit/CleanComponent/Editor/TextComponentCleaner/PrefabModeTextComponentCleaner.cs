
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;
using Unity.Linq;
using UnityEditor.SceneManagement;

namespace Modules.Devkit.CleanComponent
{
    #if UNITY_2018_3_OR_NEWER

    public class PrefabModeTextComponentCleaner : TextComponentCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            PrefabModeEventHook.OnClosePrefabModeAsObservable().Subscribe(x => ClosePrefabMode(x));
        }

        private static void ClosePrefabMode(PrefabStage prefabStage)
        {
            var modified = false;
            var gameObjects = prefabStage.prefabContentsRoot.DescendantsAndSelf();

            foreach (var gameObject in gameObjects)
            {
                modified |= ModifyTextComponent(gameObject);
            }

            if (modified)
            {
                var prefabRoot = prefabStage.prefabContentsRoot;
                var assetPath = prefabStage.prefabAssetPath;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            }
        }
    }

    #endif
}
