
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.ScriptableObjects
{
    public abstract class SingletonScriptableObject<T> : ReloadableScriptableObject where T : SingletonScriptableObject<T>
    {
        //----- params -----

        //----- field -----

        [NonSerialized]
        protected static T instance = null;

        //----- property -----

        public static T Instance
        {
            get
            {
                if (UnityUtility.IsNull(instance))
                {
                    instance = LoadInstance();

                    if (instance != null)
                    {
                        instance.OnLoadInstance();
                    }
                }

                return instance;
            }
        }

        //----- method -----

        void Awake()
        {
            if (!UnityUtility.IsNull(instance) && instance != this)
            {
                EditorApplication.delayCall += () =>
                {
                    Debug.LogWarningFormat("Can not create multiple instance.\nSingleton class : {0}", typeof(T).FullName);

                    var path = AssetDatabase.GetAssetPath(this);

                    AssetDatabase.DeleteAsset(path);
                };
            }
        }

        public override void Reload()
        {
            instance = LoadInstance();

            OnReload();
        }

        protected static T LoadInstance()
        {
            var reloadableScriptableObjectManager = ReloadableScriptableObjectManager.Instance;

            var targetAsset = UnityEditorUtility.FindAssetsByType<T>($"t:{typeof(T).FullName}").FirstOrDefault();

            if (targetAsset == null)
            {
                Debug.LogErrorFormat("Not found a matching instance.\nclass : {0}", typeof(T).FullName);
            }
            else
            {
                reloadableScriptableObjectManager.Register(targetAsset);
            }

            return targetAsset;
        }

        protected virtual void OnLoadInstance() { }
    }
}
