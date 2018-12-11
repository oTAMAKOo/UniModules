﻿﻿
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions.Devkit;
using UniRx;

namespace Modules.Devkit.EventHook
{
    public class HierarchyChangeNotification
    {
        //----- params -----

        //----- field -----

        private static Scene? currentScene = null;
        private static int[] markedObjects = new int[0];

        private static GameObject[] hierarchyObjects = new GameObject[0];

        private static Subject<GameObject[]> onCreateAsObservable = null;
        private static Subject<Unit> onDeleteAsObservable = null;
        private static Subject<Unit> onHierarchyChangedAsObservable = null;

        //----- property -----

        public static GameObject[] HierarchyObjects { get { return hierarchyObjects; } }

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            EditorApplication.hierarchyChanged += HierarchyChanged;
            EditorApplication.playModeStateChanged += x => { CollectHierarchyObjects(); };

            CollectHierarchyObjects();
        }

        private static void CollectHierarchyObjects()
        {
            // 実行中は負荷が高いので実行しない.
            if(Application.isPlaying){ return; }

            var nowScene = SceneManager.GetSceneAt(0);

            hierarchyObjects = UnityEditorUtility.FindAllObjectsInHierarchy();
            markedObjects = hierarchyObjects.Select(x => x.GetInstanceID()).ToArray();
            currentScene = nowScene;
        }

        private static void HierarchyChanged()
        {
            // 実行中は負荷が高いので実行しない.
            if(Application.isPlaying) { return; }

            if (onHierarchyChangedAsObservable != null)
            {
                onHierarchyChangedAsObservable.OnNext(Unit.Default);
            }

            var createObserver = onCreateAsObservable != null && onCreateAsObservable.HasObservers;
            var deleteObserver = onDeleteAsObservable != null && onDeleteAsObservable.HasObservers;

            if (createObserver || deleteObserver)
            {
                var nowScene = SceneManager.GetActiveScene();

                // Hierarchy上のGameObjectを検索して取得.
                hierarchyObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

                // シーンが変わっていた場合Hierarchy上のGameObjectをキャッシュ.
                if (currentScene != nowScene)
                {
                    markedObjects = hierarchyObjects.Select(x => x.GetInstanceID()).ToArray();
                    currentScene = nowScene;
                }

                // キャッシュ済みGameObjectとの差分で新規作成されたGameObjectを発見.
                var newObjects = hierarchyObjects.Where(x => !markedObjects.Any(y => y == x.GetInstanceID())).ToArray();

                // 新規作成通知.
                if (0 < newObjects.Length)
                {
                    if (onCreateAsObservable != null)
                    {
                        onCreateAsObservable.OnNext(newObjects.ToArray());
                    }
                }

                if (hierarchyObjects.Length < markedObjects.Length)
                {
                    if (onDeleteAsObservable != null)
                    {
                        onDeleteAsObservable.OnNext(Unit.Default);
                    }
                }

                markedObjects = hierarchyObjects.Select(x => x.GetInstanceID()).ToArray();
            }
        }

        public static IObservable<GameObject[]> OnCreatedAsObservable()
        {
            return onCreateAsObservable ?? (onCreateAsObservable = new Subject<GameObject[]>());
        }

        public static IObservable<Unit> OnDeleteAsObservable()
        {
            return onDeleteAsObservable ?? (onDeleteAsObservable = new Subject<Unit>());
        }

        public static IObservable<Unit> OnHierarchyChangedAsObservable()
        {
            return onHierarchyChangedAsObservable ?? (onHierarchyChangedAsObservable = new Subject<Unit>());
        }
    }
}
