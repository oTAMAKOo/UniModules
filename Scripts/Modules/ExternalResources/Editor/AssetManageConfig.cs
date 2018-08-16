
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.ScriptableObjects;
using UnityEditor.Callbacks;

namespace Modules.ExternalResource.Editor
{
    public class AssetManageConfig : ReloadableScriptableObject<AssetManageConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField, ReadOnly]
        private List<GroupInfo> groupInfos = new List<GroupInfo>();
        [SerializeField, ReadOnly]
        private List<ManageInfo> manageInfos = new List<ManageInfo>();
        [SerializeField]
        private IgnoreInfo ignoreInfo = new IgnoreInfo();

        //----- property -----

        /// <summary> 登録済みグループ </summary>
        public GroupInfo[] GroupInfos { get { return groupInfos.ToArray(); } }
        
        /// <summary> 管理情報 </summary>
        public ManageInfo[] ManageInfos { get { return manageInfos.ToArray(); } }

        /// <summary> 除外管理情報 </summary>
        public IgnoreInfo IgnoreInfo { get { return ignoreInfo; } }

        //----- method -----

        [ContextMenu("Optimisation")]
        public void Optimisation()
        {
            Reload();

            var change = false;

            if (groupInfos != null)
            {
                foreach (var groupInfo in groupInfos)
                {
                    var delete = groupInfo.manageTargetAssets.RemoveAll(x => x == null);

                    change |= delete != 0;
                }
            }

            if (manageInfos != null)
            {
                var delete = manageInfos.RemoveAll(x => x.assetObject == null);

                change |= delete != 0;
            }

            change |= ignoreInfo.Optimisation();

            if (change)
            {
                Debug.LogError("AssetManageConfig invalid data removed.");
                UnityEditorUtility.SaveAsset(this);

                Reload();
            }
        }       
    }
}