
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using UnityEditor.Experimental.SceneManagement;

namespace Modules.Devkit.EventHook
{
    #if UNITY_2018_3_OR_NEWER

    public static class PrefabModeEventHook
    {
        //----- params -----

        //----- field -----

        private static Subject<PrefabStage> onOpenPrefabMode = null;
        private static Subject<PrefabStage> onClosePrefabMode = null;
        private static Subject<GameObject> onSavingPrefabMode = null;
        private static Subject<GameObject> onSavedPrefabMode = null;

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            PrefabStage.prefabStageOpened += PrefabStageOpen;
            PrefabStage.prefabStageClosing += PrefabStageClose;
            PrefabStage.prefabSaving += PrefabStageSaving;
            PrefabStage.prefabSaved += PrefabStageSaved;
        }

        private static void PrefabStageOpen(PrefabStage prefabStage)
        {
            if (onOpenPrefabMode != null)
            {
                onOpenPrefabMode.OnNext(prefabStage);
            }
        }

        private static void PrefabStageClose(PrefabStage prefabStage)
        {
            if (onClosePrefabMode != null)
            {
                onClosePrefabMode.OnNext(prefabStage);
            }
        }

        private static void PrefabStageSaving(GameObject prefab)
        {
            if (onSavingPrefabMode != null)
            {
                onSavingPrefabMode.OnNext(prefab);
            }
        }

        private static void PrefabStageSaved(GameObject prefab)
        {
            if (onSavedPrefabMode != null)
            {
                onSavedPrefabMode.OnNext(prefab);
            }
        }

        public static IObservable<PrefabStage> OnOpenPrefabModeAsObservable()
        {
            return onOpenPrefabMode ?? (onOpenPrefabMode = new Subject<PrefabStage>());
        }

        public static IObservable<PrefabStage> OnClosePrefabModeAsObservable()
        {
            return onClosePrefabMode ?? (onClosePrefabMode = new Subject<PrefabStage>());
        }

        public static IObservable<GameObject> OnSavingPrefabModeAsObservable()
        {
            return onSavingPrefabMode ?? (onSavingPrefabMode = new Subject<GameObject>());
        }

        public static IObservable<GameObject> OnSavedPrefabModeAsObservable()
        {
            return onSavedPrefabMode ?? (onSavedPrefabMode = new Subject<GameObject>());
        }
    }

    #endif
}
