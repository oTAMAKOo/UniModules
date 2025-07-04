﻿
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    [Serializable]
    public sealed class Prefab
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameObject prefab = null;
        [SerializeField]
        private GameObject parent = null;

        //----- property -----

        public GameObject Source { get { return prefab; } }

        public GameObject Parent { get { return parent; } }

        //----- method -----

        public GameObject Instantiate(bool active = true, bool instantiateInWorldSpace = false)
        {
            if (prefab == null)
            {
                PrefabErrorMessage();
                return null;
            }

            var instance = UnityUtility.Instantiate(parent, prefab, instantiateInWorldSpace);

            UnityUtility.SetActive(instance, active);

            return instance;
        }

        public IEnumerable<GameObject> Instantiate(int count, bool active = true, bool instantiateInWorldSpace = false)
        {
            if (prefab == null)
            {
                PrefabErrorMessage();
                return null;
            }

            var instances = UnityUtility.Instantiate(parent, prefab, count, instantiateInWorldSpace).ToArray();

            instances.ForEach(x => UnityUtility.SetActive(x.gameObject, active));

            return instances;
        }

        /// <summary> インスタンスを生成. </summary>
        public T Instantiate<T>(bool active = true, bool instantiateInWorldSpace = false) where T : Component
        {
            if (prefab == null)
            {
                PrefabErrorMessage();
                return null;
            }

            var instance = UnityUtility.Instantiate<T>(parent, prefab, instantiateInWorldSpace);

            if (instance != null)
            {
                UnityUtility.SetActive(instance.gameObject, active);
            }

            return instance;
        }

        /// <summary> 複数のインスタンスを生成. </summary>
        public IEnumerable<T> Instantiate<T>(int count, bool active = true, bool instantiateInWorldSpace = false) where T : Component
        {
            if (prefab == null)
            {
                PrefabErrorMessage();
                return null;
            }

            var instances = UnityUtility.Instantiate<T>(parent, prefab, count, instantiateInWorldSpace).ToArray();

            instances.ForEach(x => UnityUtility.SetActive(x.gameObject, active));

            return instances;
        }

        private void PrefabErrorMessage()
        {
            Debug.LogError("Prefabが登録されていません");
        }
    }
}
