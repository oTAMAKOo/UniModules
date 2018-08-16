﻿﻿
using UnityEngine;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.SceneManagement
{
    public abstract partial class SceneManagement<T>
    {
        //----- params -----

        protected enum DuplicatedAction
        {
            ///<summary> コンポーネントのみ非アクティブ化. </summary>
            DisableComponent,
            ///<summary> コンポーネントがアタッチされたGameObjectを非アクティブ化. </summary>
            DisableGameObject,
            ///<summary> コンポーネントがアタッチされたGameObjectを削除. </summary>
            DestroyGameObject,
        }

        protected class DuplicatedSettings
        {
            /// <summary>
            /// シーン読み込み時に一時的に無効化するかどうかを設定する.
            /// 一時的にでも2つ以上存在するとよくない場合はtrueに設定する.
            /// </summary>
            public bool RequireSuspend { get; set; }

            /// <summary>
            /// 重複した場合の無効化方法を指定する.
            /// </summary>
            public DuplicatedAction DuplicateAction { get; set; }
        }

        //----- field -----

        private GameObject uniqueComponentsRoot = null;
        private readonly Dictionary<Type, Behaviour> capturedComponents = new Dictionary<Type, Behaviour>();

        //----- property -----

        /// <summary>
        /// 重複不可コンポーネントの定義.
        /// </summary>
        protected abstract Dictionary<Type, DuplicatedSettings> UniqueComponents { get; }

        //----- method -----

        private void CollectUniqueComponents(GameObject[] rootObjects)
        {
            if (uniqueComponentsRoot == null)
            {
                uniqueComponentsRoot = new GameObject("UniqueComponents");
                UnityEngine.Object.DontDestroyOnLoad(uniqueComponentsRoot);
            }

            foreach (var uniqueComponent in UniqueComponents)
            {
                var key = uniqueComponent.Key;

                foreach (var rootObject in rootObjects)
                {
                    var allObjects = rootObject.DescendantsAndSelf().ToArray();

                    var components = allObjects
                        .SelectMany(x => x.GetComponents(key))
                        .OfType<Behaviour>()
                        .ToArray();

                    // 対象のコンポーネントがなければなにもしない.
                    if (components.IsEmpty()) { continue; }

                    // 管理中のコンポーネントがなければとりあえず1つ管理する.
                    if (!capturedComponents.ContainsKey(key))
                    {
                        var component = components.First();

                        capturedComponents[key] = component;

                        // 既に回収済みオブジェクトの子階層にある場合は親オブジェクトの変更は行わない.
                        var captured = uniqueComponentsRoot.Descendants().Contains(component.gameObject);

                        if (!captured)
                        {
                            UnityUtility.SetParent(component.gameObject, uniqueComponentsRoot);
                        }
                    }

                    // 管理中のコンポーネントしかない場合終了.
                    if (components.Length == 1 && components.First() == capturedComponents[key]) { continue; }

                    // 管理外のコンポーネント
                    foreach (var component in components.Where(x => x != capturedComponents[key]).Where(x => x.enabled))
                    {
                        switch (uniqueComponent.Value.DuplicateAction)
                        {
                            case DuplicatedAction.DisableComponent:
                                component.enabled = false;
                                break;
                            case DuplicatedAction.DisableGameObject:
                                UnityUtility.SetActive(component.gameObject, false);
                                break;
                            case DuplicatedAction.DestroyGameObject:
                                UnityUtility.SafeDelete(component.gameObject);
                                break;
                        }
                    }
                }
            }
        }

        private void SetEnabledForCapturedComponents(bool enabled)
        {
            foreach (var settings in UniqueComponents.Where(x => x.Value.RequireSuspend))
            {
                var component = capturedComponents.GetValueOrDefault(settings.Key);

                if (component == null) { continue; }

                component.enabled = enabled;
            }
        }
    }
}