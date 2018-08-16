﻿
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Devkit.Prefs;
using UniRx;

namespace Modules.Devkit.EventHook
{
    public abstract class AdditionalComponent
    {
        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool("AdditionalComponentPrefs-enable", true); }
                set { ProjectPrefs.SetBool("AdditionalComponentPrefs-enable", value); }
            }
        }

        public class RequireComponentSettings
        {
            public Type Type { get; set; }
            public ComponentSettings ComponentSettings { get; set; }

            public RequireComponentSettings(Type type, ComponentSettings componentSettings)
            {
                Type = type;
                ComponentSettings = componentSettings;
            }
        }

        public class ComponentSettings
        {
            public Type Component { get; set; }
            public bool RequireOrderChange { get; set; }

            public ComponentSettings(Type component, bool requireOrderChange)
            {
                Component = component;
                RequireOrderChange = requireOrderChange;
            }
        }

        protected static void RegisterRequireComponents(ILookup<Type, ComponentSettings> requireSettings)
        {
            HierarchyChangeNotification.OnCreatedAsObservable().Subscribe(newGameObjects =>
            {
                // 実行中は追加しない.
                if (Application.isPlaying) { return; }

                foreach (var newGameObject in newGameObjects)
                {
                    var isPrefab = PrefabUtility.GetPrefabParent(newGameObject) != null || PrefabUtility.GetPrefabObject(newGameObject) != null;

                    // Prefabは処理しない.
                    if (isPrefab) { continue; }

                    // 子階層も走査.
                    var targetObjects = newGameObject.DescendantsAndSelf().ToArray();

                    foreach (var targetObject in targetObjects)
                    {
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
                                            SetScriptOrder(targetObject, targetComponent, component);
                                        }

                                        EditorUtility.SetDirty(targetObject);
                                        Debug.LogFormat("Attached Component: [ {0} ] {1}", component.GetType(), targetObject.transform.name);
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// T1の上にT2のコンポーネントを移動する.
        /// </summary>
        private static void SetScriptOrder(GameObject gameObject, Component parent, Component target)
        {
            var components = new List<Component>(gameObject.GetComponents<Component>());

            var index = components.IndexOf(parent);
            var targetIndex = components.IndexOf(target);

            if (index < targetIndex)
            {
                for (var i = index; i < targetIndex; ++i)
                {
                    ComponentUtility.MoveComponentUp(target);
                }
            }
            else
            {
                for (var i = targetIndex; i < index; ++i)
                {
                    ComponentUtility.MoveComponentDown(target);
                }
            }
        }
    }
}