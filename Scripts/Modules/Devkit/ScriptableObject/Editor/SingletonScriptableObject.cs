
using UnityEngine;
using UnityEditor;
using System.Linq;

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
            var targetAsset = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).FullName))
                    .Select(x => AssetDatabase.GUIDToAssetPath(x))
                    .Select(x => AssetDatabase.LoadAssetAtPath<T>(x))
                    .FirstOrDefault(x => x != null);

            if (targetAsset == null)
            {
                Debug.LogErrorFormat("Not found a matching instance.\nclass : {0}", typeof(T).FullName);
            }

            return targetAsset;
        }
    }
}
