
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.ScriptableObjects
{
	public abstract class SingletonScriptableObject<T> : UnityEngine.ScriptableObject where T : SingletonScriptableObject<T>
    {
        //----- params -----

        //----- field -----

        protected static T instance = null;

        //----- property -----

        public static T Instance { get { return instance ?? (instance = LoadInstance()); } }

        //----- method -----

        void Awake()
        {
            var target = LoadInstance();

            if (target != null && target != this)
            {
                EditorApplication.delayCall += () =>
                {
                    Debug.LogWarningFormat("Can not create multiple instance.\nSingleton class : {0}", typeof(T).FullName);

                    var path = AssetDatabase.GetAssetPath(this);
                    AssetDatabase.DeleteAsset(path);
                };
            }
        }

        public static void Reload()
        {
            instance = LoadInstance();
        }

        protected static T LoadInstance()
        {
            return UnityEditorUtility.FindAssetsByType<T>().FirstOrDefault();
        }
    }
}
