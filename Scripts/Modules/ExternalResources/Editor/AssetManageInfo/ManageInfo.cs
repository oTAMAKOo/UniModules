﻿﻿
using UnityEngine;
using System;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    [Serializable]
    public class ManageInfo
    {
        public enum NameType
        {
            /// <summary> 管理アセット名. </summary>
            ManageAssetName,
            /// <summary> 子アセット名. </summary>
            ChildAssetName,
            /// <summary> プレフィックス + 子アセット名. </summary>
            PrefixAndChildAssetName,
            /// <summary> 指定文字列. </summary>
            Specified,
        }

        /// <summary> 対象Asset </summary>
        public Object assetObject = null;

        /// <summary> アセットバンドル対象か </summary>
        public bool isAssetBundle = true;

        /// <summary> 命名タイプ </summary>
        public NameType assetBundleNameType = NameType.ManageAssetName;

        /// <summary> 命名文字列 </summary>
        public string assetBundleNameStr = null;

        /// <summary> タグ </summary>
        public string tag = null;

        /// <summary> コメント </summary>
        public string comment = null;

        public ManageInfo(Object assetObject)
        {
            this.assetObject = assetObject;
        }

        public ManageInfo(ManageInfo source)
        {
            this.assetObject = source.assetObject;
            this.isAssetBundle = source.isAssetBundle;
            this.assetBundleNameType = source.assetBundleNameType;
            this.assetBundleNameStr = source.assetBundleNameStr;
            this.tag = source.tag;
            this.comment = source.comment;
        }
    }
}
