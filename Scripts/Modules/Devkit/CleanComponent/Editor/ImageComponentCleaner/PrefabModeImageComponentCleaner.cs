
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;

namespace Modules.Devkit.CleanComponent
{
    public class PrefabModeImageComponentCleaner : ImageComponentCleaner
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
                modified |= ModifyImageComponent(gameObject);
            }

            if (modified)
            {
                var prefabRoot = prefabStage.prefabContentsRoot;
                var assetPath = prefabStage.prefabAssetPath;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            }
        }
    }
}
