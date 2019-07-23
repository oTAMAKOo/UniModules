﻿﻿
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using UniRx;

namespace Modules.Devkit.EventHook
{
    public class HierarchyChangeNotification
    {
        //----- params -----

        //----- field -----

        private static Scene? currentScene = null;
        private static GameObject[] hierarchyObjects = null;

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
            if (Application.isPlaying){ return; }
            
            currentScene = SceneManager.GetSceneAt(0);

            hierarchyObjects = UnityEditorUtility.FindAllObjectsInHierarchy();
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

                if (hierarchyObjects != null)
                {
                    hierarchyObjects = hierarchyObjects.Where(x => !UnityUtility.IsNull(x)).ToArray();
                }
                
                if (hierarchyObjects == null || hierarchyObjects.IsEmpty() || currentScene != nowScene)
                {
                    CollectHierarchyObjects();
                }
                else
                {
                    // Hierarchy上のGameObjectを検索して取得.
                    var objects = UnityEditorUtility.FindAllObjectsInHierarchy();

                    // キャッシュ済みGameObjectとの差分で新規作成されたGameObjectを発見.
                    var newObjects = objects.Where(x => hierarchyObjects.All(y => x != y)).ToArray();

                    // 新規作成通知.
                    if (0 < newObjects.Length)
                    {
                        if (onCreateAsObservable != null)
                        {
                            onCreateAsObservable.OnNext(newObjects.ToArray());
                        }
                    }

                    if (objects.Length < hierarchyObjects.Length)
                    {
                        if (onDeleteAsObservable != null)
                        {
                            onDeleteAsObservable.OnNext(Unit.Default);
                        }
                    }

                    hierarchyObjects = objects;
                }
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
