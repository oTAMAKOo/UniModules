
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.ExternalResource.Editor
{
    public enum AssetBundleNamingRule
    {
        None = 0,

        /// <summary> 管理アセット名. </summary>
        ManageAssetName,
        /// <summary> 子アセット名. </summary>
        ChildAssetName,
        /// <summary> プレフィックス + 子アセット名. </summary>
        PrefixAndChildAssetName,
        /// <summary> 指定文字列. </summary>
        Specified,
    }

    [Serializable]
    public sealed class ManageInfo
    {
        /// <summary> GUID </summary>
        public string guid = null;
        /// <summary> カテゴリ </summary>
        public string category = null;
        /// <summary> タグ </summary>
        public string tag = null;
        /// <summary> コメント </summary>
        public string comment = null;

        /// <summary> アセットバンドル対象か </summary>
        public bool isAssetBundle = true;
        /// <summary> 命名タイプ </summary>
        public AssetBundleNamingRule assetBundleNamingRule = AssetBundleNamingRule.ManageAssetName;
        /// <summary> 命名文字列 </summary>
        public string assetBundleNameStr = null;
    }
}
