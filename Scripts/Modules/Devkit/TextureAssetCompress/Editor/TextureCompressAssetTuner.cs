
using UnityEditor;
using System.Linq;
using Extensions;
using Modules.Devkit.AssetTuning;

namespace Modules.Devkit.TextureAssetCompress
{
    public class TextureCompressAssetTuner : TextureAssetTuner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnPreprocessTexture(string assetPath, bool isFirstImport)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (!IsCompressTarget(assetPath)) { return; }

            SetCompressionSettings(textureImporter);
        }

        public static bool IsCompressTarget(string assetPath)
        {
            var settings = TextureAssetCompressConfigs.Instance;

            if (settings == null) { return false; }

            assetPath = PathUtility.ConvertPathSeparator(assetPath);

            var targetPaths = settings.CompressFolders
                .Where(x => x != null)
                .Select(x => AssetDatabase.GetAssetPath(x));

            foreach (var targetPath in targetPaths)
            {
                var path = PathUtility.ConvertPathSeparator(targetPath);

                if (assetPath.StartsWith(path + PathUtility.PathSeparator)) { return true; }
            }

            return false;
        }
    }
}
