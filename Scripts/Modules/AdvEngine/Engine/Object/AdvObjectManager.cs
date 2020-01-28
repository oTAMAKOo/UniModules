
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Unity.Linq;

namespace Modules.AdvKit
{
    public sealed class AdvObjectManager : LifetimeDisposable
    {
        //----- params -----

        private const string RootObjectIdentifier = "_root";

        //----- field -----

        private GameObject rootObject = null;

        private AdvObjectSetting advObjectSetting = null;

        private Dictionary<string, AdvObject> advObjects = null;

        private bool initialized = false;

        //----- property -----

        public GameObject RootObject { get { return rootObject; } }

        //----- method -----

        public void Initialize(GameObject rootObject, AdvObjectSetting advObjectSetting)
        {
            if (initialized) { return; }

            this.rootObject = rootObject;
            this.advObjectSetting = advObjectSetting;

            UnityUtility.GetOrAddComponent<AdvObject>(rootObject);

            advObjects = new Dictionary<string, AdvObject>();

            initialized = true;
        }

        public T Create<T>(string identifier, string parentName = null) where T : AdvObject
        {
            if (identifier == RootObjectIdentifier)
            {
                Debug.LogWarningFormat("{0} is rootobject identifier can not be used.", identifier);
                return null;
            }

            if (advObjects.ContainsKey(identifier))
            {
                Debug.LogWarningFormat("AdvObject already exists. [{0}]", identifier);
                return null;
            }
            
            var prefab = advObjectSetting.GetPrefab<T>();

            if (prefab == null)
            {
                Debug.LogWarningFormat("Prefab find failed. [{0}]", typeof(T).Name);
                return null;
            }
            
            var parent = rootObject;

            if (!string.IsNullOrEmpty(parentName))
            {
                var obj = rootObject.Child(parentName);

                if (obj != null)
                {
                    parent = obj;
                }
                else
                {
                    Debug.LogWarningFormat("Parent object find failed. [{0}]", parentName);
                    return null;
                }
            }

            var advObject = UnityUtility.Instantiate<T>(parent, prefab);

            advObjects.Add(identifier, advObject);

            advObject.OnChangePriorityAsObservable()
                .Subscribe(_ => UpdateObjectsPriority())
                .AddTo(Disposable);

            advObject.Initialize(identifier);

            return advObject;
        }

        public void Delete(string identifier)
        {
            var advObject = advObjects.GetValueOrDefault(identifier);

            advObjects.Remove(identifier);

            UnityUtility.SafeDelete(advObject);
        }

        public void DeleteAll()
        {
            foreach (var item in advObjects.Values)
            {
                UnityUtility.SafeDelete(item);
            }

            advObjects.Clear();
        }

        public T Get<T>(string identifier) where T : AdvObject
        {
            if (identifier == RootObjectIdentifier)
            {
                return UnityUtility.GetComponent<AdvObject>(rootObject) as T;
            }

            return advObjects.GetValueOrDefault(identifier) as T;
        }

        public void ResetRoot()
        {
            var rt = rootObject.transform as RectTransform;

            if (rt != null)
            {
                rt.Reset();
            }
        }

        private void UpdateObjectsPriority()
        {
            var items = advObjects.Values
                .Where(x => !UnityUtility.IsNull(x))
                .OrderBy(x => x.Priority)
                .ToArray();

            for (var i = 0; i < items.Length; i++)
            {
                items[i].transform.SetSiblingIndex(i);
            }
        }
    }
}

#endif
