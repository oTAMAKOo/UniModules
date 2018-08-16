﻿
#if ENABLE_CRIWARE
﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Generators;
using System.Text;

namespace Modules.CriWare.Editor
{
	public class CriAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(string exportPath, string externalResourcesPath)
        {
            CopyCriManagedFiles(exportPath, externalResourcesPath);
        }

        /// <summary>
        /// CriAssetをアセットバンドルの出力先にコピー.
        /// </summary>
        /// <param name="exportPath"></param>
        /// <param name="externalResourcesPath"></param>
        private static void CopyCriManagedFiles(string exportPath, string externalResourcesPath)
        {
            var assetPaths = AssetDatabase.FindAssets(string.Empty, new string[] { externalResourcesPath })
                            .Select(x => AssetDatabase.GUIDToAssetPath(x))
                            .ToArray();

            foreach (var assetPath in assetPaths)
            {
                var path = assetPath.Replace(externalResourcesPath, string.Empty);

                var source = PathUtility.Combine(UnityPathUtility.GetProjectFolderPath(), assetPath);
                var dest = PathUtility.Combine(new string[] { exportPath, CriAssetDefinition.CriAssetFolder, path });

                if (PathUtility.GetFilePathType(source) == PathUtility.FilePathType.File)
                {
                    var extension = Path.GetExtension(source);

                    if (CriAssetDefinition.AssetAllExtensions.Contains(extension))
                    {
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
    }
}

#endif