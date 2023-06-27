
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using Extensions;

namespace Modules.ObjectPool
{
    public sealed class ObjectPool<T> where T : Component
    {
        //----- params -----

        //----- field -----

        private GameObject instance = null;

        private GameObject prefab = null;
        private Queue<T> cachedObjects = new Queue<T>();

        private Subject<T> onCreate = null;
        private Subject<T> onGet = null;
        private Subject<T> onRelease = null;

        //----- property -----

        public GameObject Instance { get { return instance; } }

        public int Count { get { return cachedObjects.Count; } }

        //----- method -----

        public ObjectPool(GameObject poolParent, GameObject prefab, string poolName)
        {
            this.prefab = prefab;

            instance = new GameObject(string.Format("[Pooled]: {0}", poolName));
            instance.transform.SetParent(poolParent.transform);

            instance.OnDestroyAsObservable()
                .Subscribe(_ =>
                    {
                        cachedObjects.Clear();
                    })
                .AddTo(instance);
        }

        public T Get(GameObject parent)
        {
            var target = cachedObjects.Any() ? cachedObjects.Dequeue() : null;

            if (target != null && UnityUtility.IsNull(target.gameObject))
            {
                target = null;
            }

            if (target == null)
            {
                target = UnityUtility.Instantiate<T>(instance, prefab);

                if (onCreate != null)
                {
                    onCreate.OnNext(target);
                }
            }

            if (parent != null)
            {
                UnityUtility.SetParent(target.gameObject, parent);
            }

            if (onGet != null)
            {
                onGet.OnNext(target);
            }

            return target;
        }

        public void Release(T target)
        {
            if (target == null || UnityUtility.IsNull(target.gameObject)) { return; }

            if (!cachedObjects.Contains(target))
            {
                if (onRelease != null)
                {
                    onRelease.OnNext(target);
                }
                
                UnityUtility.SetActive(target, false);
                UnityUtility.SetParent(target.gameObject, instance);

                target.transform.Reset();

                cachedObjects.Enqueue(target);
            }
        }

        public void Resize(int count)
        {
            if (instance == null) { return; }

            if (count == cachedObjects.Count) { return; }

            if (count < cachedObjects.Count)
            {
                var deleteCount = cachedObjects.Count - count;

                for (var i = 0; i < deleteCount; i++)
                {
                    var item = cachedObjects.Dequeue();
                    UnityUtility.DeleteGameObject(item);
                }
            }
            else
            {
                var addCount = count - cachedObjects.Count;

                var items = UnityUtility.Instantiate<T>(instance, prefab, addCount);

                foreach (var item in items)
                {
                    Release(item);
                }
            }
        }

        public IObservable<T> OnCreateInstanceAsObservable()
        {
            return onCreate ?? (onCreate = new Subject<T>());
        }

        public IObservable<T> OnGetInstanceAsObservable()
        {
            return onGet ?? (onGet = new Subject<T>());
        }

        public IObservable<T> OnReleaseInstanceAsObservable()
        {
            return onRelease ?? (onRelease = new Subject<T>());
        }
    }
}
