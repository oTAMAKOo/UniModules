
using UnityEngine;
using UnityEditor;
using System;
using UniRx;
using Extensions.Devkit;

namespace Modules.Devkit.EventHook
{
    public sealed class PrefabApplyHook : UnityEditor.AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

        private static Subject<GameObject> onApplyPrefab = new Subject<GameObject>();

        //----- property -----

        //----- method -----

        private static string[] OnWillSaveAssets(string[] paths)
        {
            if(!onApplyPrefab.HasObservers) { return paths; }

            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;

                if (prefab == null) { continue; }

                var isPrefab = UnityEditorUtility.IsPrefab(prefab);

                if (!isPrefab) { continue; }

                onApplyPrefab.OnNext(prefab);                
            }

            return paths;
        }

        public static IObservable<GameObject> OnApplyPrefabAsObservable()
        {
            return onApplyPrefab;
        }
    }
}
