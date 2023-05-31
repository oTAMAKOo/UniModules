
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using System.IO;
using System.Linq;
using Extensions;
using Modules.Devkit.Project;
using Modules.ExternalAssets;

namespace Modules.CriWare.Editor
{
    public sealed class CriAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> CriAssetをアセットバンドルの出力先にコピー. </summary>
        public static void Generate(string exportPath, AssetInfoManifest assetInfoManifest)
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null){ return; }

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;

            bool IsCriAssetInfo(AssetInfo assetInfo)
            {
                if (string.IsNullOrEmpty(assetInfo.FileName)) { return false; }

                if (assetInfo.IsAssetBundle){ return false; }

                var extension = Path.GetExtension(assetInfo.ResourcePath);

                return CriAssetDefinition.AssetAllExtensions.Contains(extension);
            }

            var assetInfos = assetInfoManifest.GetAssetInfos().Where(x => IsCriAssetInfo(x));

            foreach (var assetInfo in assetInfos)
            {
                var source = PathUtility.Combine(UnityPathUtility.GetProjectFolderPath(), externalAssetPath, assetInfo.ResourcePath);
                var dest = PathUtility.Combine(exportPath, assetInfo.FileName);

                File.Copy(source, dest, true);
            }
        }

        /// <summary> 出力先のCriAssetを全削除. </summary>
        public static void Clean(string exportPath)
        {
            bool IsCriFile(string path)
            {
                var extension = Path.GetExtension(path);

                return CriAssetDefinition.AssetAllExtensions.Contains(extension);
            }

            var files = Directory.EnumerateFiles(exportPath, "*.*", SearchOption.AllDirectories)
                .Where(x => IsCriFile(x))
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            foreach (var file in files)
            {
                File.Delete(file);
            }
        }
    }
}

#endif
