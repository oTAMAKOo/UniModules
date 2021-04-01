
using UnityEditor;
using System.Linq;

namespace Modules.Devkit.Inspector
{
    public sealed class FolderRegisterScrollView : AssetRegisterScrollView
    {
        //----- params -----

        //----- field -----

        //----- property -----

        /// <summary>
        /// 既に子階層のフォルダが登録済みの時そのフォルダを除外するか.
        /// </summary>
        public bool RemoveChildrenFolder { get; set; }

        //----- method -----

        public FolderRegisterScrollView(string title, string headerKey) : base(title, headerKey) { }

        // 登録された際に削除されるアセット情報取得.
        protected override void ValidateContent(AssetInfo newAssetInfo)
        {
            base.ValidateContent(newAssetInfo);

            // フォルダではなかったら除外
            if (!AssetDatabase.IsValidFolder(newAssetInfo.assetPath))
            {
                EditorUtility.DisplayDialog("Error", "Require registration is folder.", "Close");

                Contents = Contents.Where(x => x.guid != newAssetInfo.guid).ToArray();
            }
        }
    }
}
