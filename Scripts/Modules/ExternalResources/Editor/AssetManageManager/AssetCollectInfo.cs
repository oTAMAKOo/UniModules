
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;

namespace Modules.ExternalResource.Editor
{
	public class AssetCollectInfo
    {
        //----- params -----

        //----- field -----

        private AssetManageManager assetManageManager = null;

        //----- property -----

        public string AssetPath { get; private set; }
               
        public AssetInfo AssetInfo { get; private set; }

        public ManageInfo ManageInfo { get; private set; }

        public IgnoreType? Ignore { get; private set; }

        //----- method -----

        public AssetCollectInfo(AssetManageManager assetManageManager, string assetPath, AssetInfo assetInfo, ManageInfo manageInfo, IgnoreType? ignoreType)
        {
            this.assetManageManager = assetManageManager;

            AssetPath = assetPath;
            AssetInfo = assetInfo;
            ManageInfo = manageInfo;
            Ignore = ignoreType;
        }

        public bool ApplyAssetBundleName()
        {
            var result = false;

            if (string.IsNullOrEmpty(AssetPath) || !AssetPath.StartsWith(UnityPathUtility.AssetsFolder))
            {
                return false;
            }

            // 管理外判定.
            if (Ignore.HasValue)
            {
                // 管理対象外の場合変更しない.
                if (Ignore == IgnoreType.IgnoreManage) { return false; }

                // 除外対象: フォルダ / ファイル / 拡張子.
                result = assetManageManager.SetAssetBundleName(AssetPath, string.Empty);
            }
            else
            {
                if(AssetInfo != null && !string.IsNullOrEmpty(AssetInfo.AssetBundleName))
                {
                    result = assetManageManager.SetAssetBundleName(AssetPath, AssetInfo.AssetBundleName);
                }
                else
                {
                    result = assetManageManager.SetAssetBundleName(AssetPath, string.Empty);
                }
            }

            return result;
        }
    }
}
