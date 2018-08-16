
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Extensions;

namespace Modules.AssetBundles.Editor
{
    public class BuildScript
    {
        /// <summary> 全てのアセットバンドルをビルド </summary>
        public static void BuildAllAssetBundles(string exportPath, BuildAssetBundleOptions bundleOptions = BuildAssetBundleOptions.None)
        {
            var outputPath = CreateExportFolder(exportPath);

            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            BuildPipeline.BuildAssetBundles(outputPath, bundleOptions, targetPlatform);
        }

        public static string GetExportFolderPath(string exportPath)
        {
            return PathUtility.Combine(exportPath, AssetBundleManager.AssetBundlesFolder)
                .Replace(UnityPathUtility.GetProjectFolderPath(), string.Empty);
        }

        private static string CreateExportFolder(string exportPath)
        {
            var outputPath = GetExportFolderPath(exportPath);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            return outputPath;
        }
    }
}