
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using Extensions;
using UnityEditor.Callbacks;

namespace Modules.Devkit.ScriptableObjects
{
    public abstract class ReloadableScriptableObject<T> : SingletonScriptableObject<T> where T : ReloadableScriptableObject<T>
    {
        //----- params -----

        //----- field -----

        private DateTime? lastWriteTime = null;

        //----- property -----

        public new static T Instance { get { return GetInstance(); } }

        //----- method -----

        private static T GetInstance()
        {
            if (instance == null)
            {
                instance = LoadInstance();
            }

            if (instance == null) { return null; }

            var assetPath = AssetDatabase.GetAssetPath(instance);
            var path = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

            var time = File.GetLastWriteTime(path);

            var update = !instance.lastWriteTime.HasValue || instance.lastWriteTime.Value != time;
            
            if(update)
            {
                instance = LoadInstance();
                instance.lastWriteTime = time;
            }

            return instance;
        }
    }
}
