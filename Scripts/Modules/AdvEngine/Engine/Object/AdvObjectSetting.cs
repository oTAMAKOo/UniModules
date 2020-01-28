
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Serialize;

namespace Modules.AdvKit
{
    public class AdvObjectSetting : ScriptableObject
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private AdvObject[] advObjectPrefabs = null;

        private Dictionary<Type, GameObject> advObjectLibrary = null;

        //----- property -----

        //----- method -----

        private void Setup()
        {
            advObjectLibrary = new Dictionary<Type, GameObject>();

            foreach (var advObject in advObjectPrefabs)
            {
                advObjectLibrary.Add(advObject.GetType(), advObject.gameObject);
            }
        }

        public GameObject GetPrefab<T>()
        {
            if (advObjectLibrary == null)
            {
                Setup();
            }

            var prefab = advObjectLibrary.GetValueOrDefault(typeof(T));
            
            return prefab;
        }
    }
}