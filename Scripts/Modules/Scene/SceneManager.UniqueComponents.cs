﻿
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using Extensions;

namespace Modules.Scene
{
    public abstract partial class SceneManager<T>
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

        protected sealed class DuplicatedSettings
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

        public void RegisterUniqueComponent<TComponent>(TComponent target) where TComponent : Behaviour
        {
            RegisterUniqueComponent(typeof(TComponent), target);
        }

        public void RegisterUniqueComponent(Type type, Behaviour target)
        {
            if (type == null){ return; }

            if (UnityUtility.IsNull(target)){ return; }

            var capturedComponent = capturedComponents.GetValueOrDefault(type);

            if (capturedComponent == target){ return; }

            UnityUtility.SafeDelete(capturedComponent, true);

            target.OnDestroyAsObservable()
                .Subscribe(_ => capturedComponents.Remove(type))
                .AddTo(Disposable);

            capturedComponents[type] = target;

            // 既に回収済みオブジェクトの子階層にある場合は親オブジェクトの変更は行わない.
            var captured = uniqueComponentsRoot.Descendants().Contains(target.gameObject);

            if (!captured)
            {
                UnityUtility.SetParent(target.gameObject, uniqueComponentsRoot);
            }
        }

        public void CollectUniqueComponents()
        {
            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                var rootObjects = scene.GetRootGameObjects();

                CollectUniqueComponents(rootObjects);
            }
        }

        private void CollectUniqueComponents(GameObject[] rootObjects)
        {
            if (uniqueComponentsRoot == null)
            {
                uniqueComponentsRoot = new GameObject("UniqueComponents");
                UnityEngine.Object.DontDestroyOnLoad(uniqueComponentsRoot);
            }

            var allComponents = new List<Behaviour>();

            foreach (var rootObject in rootObjects)
            {
                var components = rootObject.DescendantsAndSelf().SelectMany(x => x.GetComponents<Behaviour>());

                allComponents.AddRange(components);
            }

            foreach (var uniqueComponent in UniqueComponents)
            {
                var key = uniqueComponent.Key;
                
                var components = allComponents.Where(x => x.GetType() == key).ToArray();

                // 対象のコンポーネントがなければなにもしない.
                if (components.IsEmpty()) { continue; }

                var capturedComponent = capturedComponents.GetValueOrDefault(key);

                // 管理中のコンポーネントがなければとりあえず1つ管理する.
                if (UnityUtility.IsNull(capturedComponent))
                {
                    var component = components.First();

                    RegisterUniqueComponent(key, component);
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
