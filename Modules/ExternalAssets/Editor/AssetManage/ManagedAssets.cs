
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.ScriptableObjects;

namespace Modules.ExternalAssets
{
    public sealed class ManagedAssets : SingletonScriptableObject<ManagedAssets>
    {
        //----- params -----

        //----- field -----
        
        [SerializeField, HideInInspector]
        private ManageInfo[] manageInfos = new ManageInfo[0];

        private Dictionary<string, ManageInfo> manageInfoDictionary = null;

        //----- property -----

        //----- method -----

		public void SetManageInfos(ManageInfo[] manageInfos)
        {
            this.manageInfos = manageInfos
                .Where(x => x != null)
                .Where(x => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(x.guid)))
                .ToArray();

            manageInfoDictionary = this.manageInfos.ToDictionary(x => x.guid);

            UnityEditorUtility.SaveAsset(this);
        }

        public ManageInfo[] GetAllInfos()
        {
            return manageInfos;
        }

        public ManageInfo[] GetGroupInfos(string group)
        {
            if (string.IsNullOrEmpty(group)){ return GetAllInfos(); }

            return manageInfos.Where(x => x.group == group).ToArray();
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
