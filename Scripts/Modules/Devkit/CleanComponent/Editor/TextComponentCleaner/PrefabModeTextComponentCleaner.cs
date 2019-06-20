﻿
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using Unity.Linq;
using System.Linq;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;
using Modules.GameText.Editor;

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
            var gameObjects = prefabStage.prefabContentsRoot.DescendantsAndSelf().ToArray();

            if (!CheckExecute(gameObjects)) { return; }

            GameTextLoader.Reload();

            foreach (var gameObject in gameObjects)
            {
                ModifyTextComponent(gameObject);
            }

            var prefabRoot = prefabStage.prefabContentsRoot;
            var assetPath = prefabStage.prefabAssetPath;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);   
        }
    }

    #endif
}
