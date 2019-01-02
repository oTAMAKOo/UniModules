
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Devkit.EventHook
{
    public class ComponentTuningInPrefabMode : ComponentTuning
    {
        //----- params -----

        //----- field -----

        private static GameObject[] prefabModeObjects = null;

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            EditorApplication.hierarchyChanged += ModifyTuneComponents;

            PrefabModeEventHook.OnOpenPrefabModeAsObservable().Subscribe(x => OnOpenPrefabMode(x));

            PrefabModeEventHook.OnClosePrefabModeAsObservable().Subscribe(x => OnClosePrefabMode(x));

            var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (currentPrefabStage == null) { return; }

            prefabModeObjects = GetStateGameObjects(currentPrefabStage.stageHandle);
        }

        private static void OnOpenPrefabMode(PrefabStage prefabStage)
        {
            if (prefabStage == null) { return; }

            prefabModeObjects = GetStateGameObjects(prefabStage.stageHandle);
        }

        private static void OnClosePrefabMode(PrefabStage prefabStage)
        {
            prefabModeObjects = null;
        }

        private static void ModifyTuneComponents()
        {
            // 実行中は追加しない.
            if (Application.isPlaying) { return; }

            var currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (currentPrefabStage == null) { return; }

            var gameObjects = GetStateGameObjects(currentPrefabStage.stageHandle);

            var newGameObjects = gameObjects.Where(x => !prefabModeObjects.Contains(x)).ToArray();

            TuneComponents(newGameObjects, CheckExecute);

            prefabModeObjects = gameObjects;
        }

        private static bool CheckExecute(GameObject target)
        {
            var nestPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(target);

            // 他のPrefabが追加された時は新規オブジェクト扱いしない.
            if (nestPrefab != null) { return false; }

            return true;
        }

        private static GameObject[] GetStateGameObjects(StageHandle stageHandle)
        {
            return stageHandle.FindComponentsOfType<Transform>()
                .Select(x => x.gameObject)
                .Distinct()
                .ToArray();
        }
    }
}
