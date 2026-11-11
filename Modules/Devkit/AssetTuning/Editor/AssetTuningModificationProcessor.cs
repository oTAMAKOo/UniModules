
using System.IO;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public sealed class AssetTuningModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void OnWillCreateAsset(string path)
        {
            var assetTuneManager = AssetTuneManager.Instance;

            var extension = Path.GetExtension(path);

            // メタファイルパスからパスを取得.
            if (extension == UnityEditorUtility.MetaFileExtension)
            {
                path = path.Replace(UnityEditorUtility.MetaFileExtension, string.Empty);
                assetTuneManager.MarkFirstImport(path);
            }
        }
    }
}
