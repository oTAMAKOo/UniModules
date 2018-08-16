﻿﻿
using UnityEngine;
using System;

namespace Extensions
{
    [Serializable]
    public class Prefab
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        protected GameObject prefab = null;
        [SerializeField]
        protected GameObject parent = null;

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

        private void PrefabErrorMessage()
        {
            Debug.LogError("Prefabが登録されていません");
        }
    }
}