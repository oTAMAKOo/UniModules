﻿
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.EventHook
{
    public abstract class AdditionalComponent
    {
        //----- params -----

        public static class Prefs
        {
            public static bool Enable
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-Enable", true); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-Enable", value); }
            }

            public static bool LogEnable
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-LogEnable", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-LogEnable", value); }
            }
        }

        public sealed class RequireComponentSettings
        {
            public Type Type { get; set; }
            public ComponentSettings ComponentSettings { get; set; }

            public RequireComponentSettings(Type type, ComponentSettings componentSettings)
            {
                Type = type;
                ComponentSettings = componentSettings;
            }
        }

        public sealed class ComponentSettings
        {
            public Type Component { get; set; }
            public bool RequireOrderChange { get; set; }

            public ComponentSettings(Type component, bool requireOrderChange)
            {
                Component = component;
                RequireOrderChange = requireOrderChange;
            }
        }

        //----- field -----

        protected static ILookup<Type, ComponentSettings> requireSettings = null;

        //----- property -----

        //----- method -----

        public static void RegisterRequireComponents(ILookup<Type, AdditionalComponent.ComponentSettings> requireSettings)
        {
            AdditionalComponent.requireSettings = requireSettings;
        }

        protected static void AddRequireComponents(GameObject[] newGameObjects, Func<GameObject, bool> checkExecute)
        {
            // 無効化中は追加しない.
            if (!Prefs.Enable) { return; }

            // 実行中は追加しない.
            if (Application.isPlaying) { return; }
            
            foreach (var newGameObject in newGameObjects)
            {
                // 子階層も走査.
                var targetObjects = newGameObject.DescendantsAndSelf();

                foreach (var targetObject in targetObjects)
                {
                    // 実行対象か判定.
                    if (!checkExecute(targetObject)) { continue; }

                    foreach (var requireSetting in requireSettings)
                    {
                        var targetComponent = targetObject.GetComponent(requireSetting.Key);

                        if (targetComponent != null)
                        {
                            foreach (var settings in requireSetting)
                            {
                                var component = targetObject.GetComponent(settings.Component);

                                // アタッチされていない場合にアタッチする.
                                if (component == null)
                                {
                                    component = targetObject.AddComponent(settings.Component);

                                    if (settings.RequireOrderChange)
                                    {
                                        AdditionalComponentUtility.SetScriptOrder(targetObject, targetComponent, component);
                                    }

                                    EditorUtility.SetDirty(targetObject);

                                    if (Prefs.LogEnable)
                                    {
                                        Debug.LogFormat("Attached Component: [ {0} ] {1}", component.GetType(), targetObject.transform.name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
