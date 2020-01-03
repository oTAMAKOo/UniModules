
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.EventHook
{
    public abstract class ComponentTuner
    {
        public abstract Type TargetComponent { get; }
        public abstract bool Tuning(Component component);
    }

    public abstract class ComponentTuning
    {
        //----- params -----

        public static class Prefs
        {
            public static bool Enable
            {
                get { return ProjectPrefs.GetBool("ComponentTunerPrefs-Enable", true); }
                set { ProjectPrefs.SetBool("ComponentTunerPrefs-Enable", value); }
            }

            public static bool LogEnable
            {
                get { return ProjectPrefs.GetBool("ComponentTunerPrefs-Log", true); }
                set { ProjectPrefs.SetBool("ComponentTunerPrefs-Log", value); }
            }
        }

        //----- field -----
        
        private static ComponentTuner[] tuners = null;

        //----- property -----
        
        //----- method -----

        protected static void TuneComponents(GameObject[] newGameObjects, Func<GameObject, bool> checkExecute)
        {
            if (!Prefs.Enable) { return; }

            var tunedObject = new List<GameObject>();

            foreach (var newGameObject in newGameObjects)
            {
                // 子階層も走査.
                var targetObjects = newGameObject.DescendantsAndSelf().ToArray();

                foreach (var targetObject in targetObjects)
                {
                    // 実行判定.
                    if (!checkExecute(targetObject)) { continue; }

                    // 既に実行済みのオブジェクトはスキップ.
                    if (tunedObject.Contains(targetObject)) { continue; }

                    foreach (var tuner in tuners)
                    {
                        var component = targetObject.GetComponent(tuner.TargetComponent);

                        if (component != null)
                        {
                            if (tuner.Tuning(component) && Prefs.LogEnable)
                            {
                                Debug.LogFormat("Tuning Component: [ {0} ] {1}", tuner.TargetComponent.Name, component.transform.name);
                            }

                            tunedObject.Add(targetObject);
                        }
                    }
                }
            }
        }

        protected static void RegisterTuners(ComponentTuner[] tuners)
        {
            ComponentTuning.tuners = tuners;
        }
    }
}
