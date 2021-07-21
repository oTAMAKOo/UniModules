﻿
using UnityEngine;

namespace Extensions
{
    [ExecuteAlways]
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        //----- param -----

        //----- field -----

        private static T instance = null;

        //----- property -----

        public static T Instance
        {
            get
            {
                return instance ?? (instance = UnityUtility.FindObjectOfType<T>());
            }
        }

        //----- method -----

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
            }

            CheckInstance();
        }

        protected virtual void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void CheckInstance()
        {
            if (instance != this)
            {
                if (Application.isPlaying)
                {
                    UnityUtility.SafeDelete(gameObject);
                }
            }
        }

        public static T CreateInstance()
        {
            if (UnityUtility.IsNull(instance))
            {
                instance = UnityUtility.CreateGameObject<T>(null, typeof(T).Name);
            }

            return instance;
        }

        public static void DestroyInstance()
        {
            if (!UnityUtility.IsNull(instance))
            {
                UnityUtility.SafeDelete(instance.gameObject);
            }
        }
    }
}
