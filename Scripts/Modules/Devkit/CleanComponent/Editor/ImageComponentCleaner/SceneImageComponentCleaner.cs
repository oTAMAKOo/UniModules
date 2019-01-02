
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Atlas;
using Modules.Devkit.CleanComponent;
using Modules.Devkit.EventHook;
using Modules.GameText.Editor;
using Unity.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace Modules.Devkit.CleanComponent
{
    public class SceneImageComponentCleaner : ImageComponentCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            CurrentSceneSaveHook.OnSaveSceneAsObservable().Subscribe(x => Clean(x));
        }

        public static void Clean()
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            Clean(activeScene.path);
        }

        private static void Clean(string sceneAssetPath)
        {
            if (!Prefs.autoClean) { return; }

            var activeScene = EditorSceneManager.GetActiveScene();

            if (activeScene.path != sceneAssetPath) { return; }

            var rootGameObjects = activeScene.GetRootGameObjects();

            if (!CheckExecute(rootGameObjects)) { return; }

            foreach (var rootGameObject in rootGameObjects)
            {
                ModifyImageComponent(rootGameObject);
            }
        }
    }
}
