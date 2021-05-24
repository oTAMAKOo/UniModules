
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.Build
{
    public sealed class DeleteAssetSetting : ReloadableScriptableObject<DeleteAssetSetting>
    {
        [SerializeField]
        private string[] targetGuids = null;

        public IReadOnlyCollection<Object> GetTargets()
        {
            return targetGuids.Where(x => !string.IsNullOrEmpty(x))
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => AssetDatabase.LoadAssetAtPath(x, typeof(Object)))
                .ToArray();
        }
    }
}
