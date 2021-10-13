
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.Devkit.Build
{
    public sealed class DeleteAssetSetting : ScriptableObject
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string tag = null;
        [SerializeField]
        private string[] guids = null;

        //----- property -----

        public string Tag { get { return tag; } }
        
        public IReadOnlyList<string> Guids
        {
            get { return guids ?? (guids = new string[0]); }
        }

        //----- method -----

        public static IReadOnlyCollection<Object> GetTargets(string[] tags)
        {
            if (tags == null || tags.IsEmpty()){ return new Object[0]; }

            var settingGuids = AssetDatabase.FindAssets("t:DeleteAssetSetting");

            var settings = settingGuids.Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadMainAssetAtPath(x) as DeleteAssetSetting)
                .ToArray();

            var guids = settings.Where(x => tags.Contains(x.Tag))
                .SelectMany(x => x.Guids)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            var targets = guids.Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => AssetDatabase.LoadAssetAtPath(x, typeof(Object)))
                .ToArray();

            return targets;
        }
    }
}
