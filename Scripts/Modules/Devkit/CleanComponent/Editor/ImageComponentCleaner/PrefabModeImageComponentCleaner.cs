
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
            var gameObjects = prefabStage.prefabContentsRoot.DescendantsAndSelf();

            foreach (var gameObject in gameObjects)
            {
                ModifyImageComponent(gameObject);
            }

            var prefabRoot = prefabStage.prefabContentsRoot;
            var assetPath = prefabStage.prefabAssetPath;

            PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, assetPath, InteractionMode.UserAction);
        }
    }
}
