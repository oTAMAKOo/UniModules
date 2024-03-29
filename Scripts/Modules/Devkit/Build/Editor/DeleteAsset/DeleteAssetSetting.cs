﻿
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

        public string Tag
        {
            get { return string.IsNullOrEmpty(tag) ? string.Empty : tag.Trim(); }
        }
        
        public IReadOnlyList<string> Guids
        {
            get { return guids ?? (guids = new string[0]); }
        }

        //----- method -----

        public static IReadOnlyCollection<string> GetTargetAssetPaths(IEnumerable<string> tags)
        {
            if (tags == null){ return new string[0]; }

            var tagList = tags.Select(x => x.Trim()).ToList();

            if (tagList.IsEmpty()){ return new string[0]; }

            var settingGuids = AssetDatabase.FindAssets("t:DeleteAssetSetting");

            var settings = settingGuids.Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadMainAssetAtPath(x) as DeleteAssetSetting)
                .ToArray();

            var guids = settings.Where(x => tagList.Contains(x.Tag))
                .SelectMany(x => x.Guids)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            var targets = guids.Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToArray();

            return targets;
        }
    }
}
