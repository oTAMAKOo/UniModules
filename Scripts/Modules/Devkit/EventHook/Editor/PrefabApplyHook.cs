﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.EventHook
{
    public class PrefabApplyHook : UnityEditor.AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

        private static Subject<GameObject> onApplyPrefab = new Subject<GameObject>();

        //----- property -----

        //----- method -----

        public static IObservable<GameObject> OnApplyPrefabAsObservable()
        {
            return onApplyPrefab;
        }

        private static string[] OnWillSaveAssets(string[] paths)
        {
            if(!onApplyPrefab.HasObservers) { return paths; }

            var gameObject = Selection.activeGameObject;

            if (gameObject == null) { return paths; }

            var isPrefab = UnityEditorUtility.IsPrefab(gameObject);

            if (!isPrefab) { return paths; }

            onApplyPrefab.OnNext(gameObject);

            return paths;
        }
    }
}
