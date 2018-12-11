﻿﻿
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.EventHook
{
    public abstract class ComponentTuner : ScriptableObject
    {
        public abstract Type TargetComponent { get; }
        public abstract bool Tuning(Component component);
    }

    public abstract class ComponentTuning
    {
        //----- params -----

        private const string PrefsKey = "ComponentTuner-TuningLog";

        //----- field -----

        private static bool logEnable = false;
        private static IDisposable disposable = null;

        //----- property -----

        public static bool LogEnable { get { return logEnable; } }

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            logEnable = ProjectPrefs.GetBool(PrefsKey, true);
        }

        public static void ToggleTuningLog()
        {
            logEnable = !logEnable;
            ProjectPrefs.SetBool(PrefsKey, logEnable);
        }

        protected static void RegisterTuners(ComponentTuner[] tuners)
        {
            if (disposable != null)
            {
                disposable.Dispose();
            }

            disposable = HierarchyChangeNotification.OnCreatedAsObservable().Subscribe(newGameObjects =>
            {
                // 実行中は追加しない.
                if (Application.isPlaying) { return; }

                var tunedObject = new List<GameObject>();

                foreach (var newGameObject in newGameObjects)
                {
                    var isPrefab = UnityEditorUtility.IsPrefab(newGameObject);

                    // Prefabは処理しない.
                    if (isPrefab) { continue; }

                    // 子階層も走査.
                    var targetObjects = newGameObject.DescendantsAndSelf().ToArray();

                    foreach (var targetObject in targetObjects)
                    {
                        // 既に実行済みのオブジェクトはスキップ.
                        if (tunedObject.Contains(targetObject)) { continue; }

                        foreach (var tuner in tuners)
                        {
                            var component = targetObject.GetComponent(tuner.TargetComponent);

                            if (component != null)
                            {
                                if (tuner.Tuning(component) && logEnable)
                                {
                                    Debug.LogFormat("Tuning Component: [ {0} ] {1}", tuner.TargetComponent.Name, component.transform.name);
                                }

                                tunedObject.Add(targetObject);
                            }
                        }
                    }
                }
            });
        }
    }
}
