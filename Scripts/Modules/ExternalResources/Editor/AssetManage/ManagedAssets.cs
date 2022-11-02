﻿
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.ScriptableObjects;

namespace Modules.ExternalResource
{
    public sealed class ManagedAssets : ReloadableScriptableObject<ManagedAssets>
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

        public void DeleteInvalidInfo()
        {
            var removeList = new List<ManageInfo>();

            for (var i = 0; i < manageInfos.Length; i++)
            {
                var manageInfo = manageInfos[i];

                if (manageInfo == null){ continue; }

                if (string.IsNullOrEmpty(manageInfo.guid))
				{
					removeList.Add(manageInfo);
					continue;
				}
				
				var assetPath = AssetDatabase.GUIDToAssetPath(manageInfo.guid);

				if (string.IsNullOrEmpty(assetPath))
				{
					removeList.Add(manageInfo);
					continue;
				}
			}

            if (removeList.Any())
            { 
				var title = "ExternalResource ManagedAssets";
				var message = "Contain invalid manage info.\nDo you want to run cleanup?";

				var result = EditorUtility.DisplayDialog(title, message, "execute", "cancel");

				if (result)
				{
	                manageInfos = manageInfos.Where(x => !removeList.Contains(x)).ToArray();

	                UnityEditorUtility.SaveAsset(this);
				}
            }
        }
    }
}
