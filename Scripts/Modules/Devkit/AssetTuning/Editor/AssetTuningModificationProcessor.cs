
using UnityEngine;
using UnityEditor;
using System.IO;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public class AssetTuningModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void OnWillCreateAsset(string path)
        {
            var assetTuner = AssetTuner.Instance;

            var extension = Path.GetExtension(path);

            // メタファイルパスからパスを取得.
            if (extension == UnityEditorUtility.MetaFileExtension)
            {
                path = path.Replace(UnityEditorUtility.MetaFileExtension, string.Empty);
                assetTuner.MarkFirstImport(path);
            }
        }
    }
}
