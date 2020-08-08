
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.ScriptableObjects;

namespace Modules.ExternalResource.Editor
{
    public sealed class ManagedAssets : ReloadableScriptableObject<ManagedAssets>
    {
        //----- params -----

        //----- field -----
        
        [SerializeField]//, HideInInspector]
        private ManageInfo[] manageInfos = null;

        private Dictionary<string, ManageInfo> manageInfoDictionary = null;

        //----- property -----

        //----- method -----

        public void SetManageInfos(ManageInfo[] manageInfos)
        {
            this.manageInfos = manageInfos
                .Where(x => x != null)
                .Where(x => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(x.guid)))
                .ToArray();

            manageInfoDictionary = manageInfos.ToDictionary(x => x.guid);

            UnityEditorUtility.SaveAsset(this);
        }

        public ManageInfo[] GetAllInfos()
        {
            return manageInfos;
        }

        public ManageInfo[] GetCategoryInfos(string category)
        {
            if (string.IsNullOrEmpty(category)){ return GetAllInfos(); }

            return manageInfos.Where(x => x.category == category).ToArray();
        }

        public ManageInfo GetManageInfo(string assetGuid)
        {
            if (manageInfoDictionary == null)
            {
                manageInfoDictionary = manageInfos.ToDictionary(x => x.guid);
            }

            return manageInfoDictionary.GetValueOrDefault(assetGuid);
        }
    }
}
