
using System;

namespace Modules.ExternalResource
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
		/// <summary> ファイルパス(1アセット = 1アセットバンドル). </summary>
		AssetFilePath,
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
        /// <summary> ラベル </summary>
        public string[] labels = new string[0];
        /// <summary> コメント </summary>
        public string comment = null;

        /// <summary> アセットバンドル対象か </summary>
        public bool isAssetBundle = true;
        /// <summary> 命名タイプ </summary>
        public AssetBundleNamingRule assetBundleNamingRule = AssetBundleNamingRule.ChildAssetName;
        /// <summary> 命名文字列 </summary>
        public string assetBundleNameStr = null;

		public string GetLabelText()
		{
			return string.Join(", ", labels);
		}
    }
}
