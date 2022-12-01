
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC
﻿﻿
using System;
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

        /// <summary>
        /// CriAssetをアセットバンドルの出力先にコピー.
        /// </summary>
        public static void Generate(string exportPath, AssetInfoManifest assetInfoManifest)
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null){ return; }

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;

            Func<AssetInfo, bool> isCriAssetInfo = x =>
            {
                if (string.IsNullOrEmpty(x.FileName)) { return false; }

                var extension = Path.GetExtension(x.ResourcePath);

                return CriAssetDefinition.AssetAllExtensions.Contains(extension);
            };

            var assetInfos = assetInfoManifest.GetAssetInfos().Where(x => isCriAssetInfo(x)).ToArray();

            foreach (var assetInfo in assetInfos)
            {
                var source = PathUtility.Combine(new string[] { UnityPathUtility.GetProjectFolderPath(), externalAssetPath, assetInfo.ResourcePath });
                var dest = PathUtility.Combine(new string[] { exportPath, assetInfo.FileName });

                var directory = Path.GetDirectoryName(dest);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(source, dest, true);
            }
        }
    }
}

#endif
