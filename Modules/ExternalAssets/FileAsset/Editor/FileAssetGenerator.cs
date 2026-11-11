
using System.Linq;
using System.IO;
using Extensions;
using Modules.Devkit.Project;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC
using Modules.CriWare;
#endif

namespace Modules.ExternalAssets
{
    public sealed class FileAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		/// <summary> FileAssetをアセットバンドルの出力先にコピー. </summary>
        public static void Generate(string exportPath, AssetInfoManifest assetInfoManifest)
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null){ return; }

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;

			bool IsFileAssetInfo(AssetInfo assetInfo)
            {
                if (string.IsNullOrEmpty(assetInfo.FileName)) { return false; }

				if (assetInfo.IsAssetBundle){ return false; }

				#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC
				
				var extension = Path.GetExtension(assetInfo.ResourcePath);

				if (CriAssetDefinition.AssetAllExtensions.Contains(extension)){ return false; }

				#endif

                return true;
            };

            var assetInfos = assetInfoManifest.GetAssetInfos().Where(x => IsFileAssetInfo(x));

            foreach (var assetInfo in assetInfos)
            {
                var source = PathUtility.Combine(new string[] { UnityPathUtility.GetProjectFolderPath(), externalAssetPath, assetInfo.ResourcePath });
                var dest = PathUtility.Combine(new string[] { exportPath, assetInfo.FileName });

                if (!File.Exists(source)){ continue; }

				File.Copy(source, dest, true);
            }
        }
    }
}