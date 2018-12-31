
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.EventHook
{
    public static class AdditionalComponentInScene
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void RegisterRequireComponents(ILookup<Type, AdditionalComponent.ComponentSettings> requireSettings)
        {
            HierarchyChangeNotification.OnCreatedAsObservable().Subscribe(newGameObjects =>
            {
                // 実行中は追加しない.
                if (Application.isPlaying) { return; }

                foreach (var newGameObject in newGameObjects)
                {
                    var isPrefab = UnityEditorUtility.IsPrefab(newGameObject);

                    // Prefabは処理しない.
                    if (isPrefab) { continue; }

                    // 子階層も走査.
                    var targetObjects = newGameObject.DescendantsAndSelf();

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
                                            AdditionalComponentUtility.SetScriptOrder(targetObject, targetComponent, component);
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
    }
}
