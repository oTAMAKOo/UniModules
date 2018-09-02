
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions;
using Modules.ExternalResource;


namespace Modules.AssetBundles.Editor
{
    public static class BuildAssetBundle
    {
        //----- params -----

        private const string AssetBundleManifestName = "AssetBundle";

        private const string ManifestFileExtension = ".manifest";

        //----- field -----

        //----- property -----

        //----- method -----

        private static string GetAssetBundleOutputPath(string exportPath)
        {
            return PathUtility.Combine(exportPath, AssetBundleManager.AssetBundlesFolder)
                .Replace(UnityPathUtility.GetProjectFolderPath(), string.Empty);
        }

        /// <summary> 全てのアセットバンドルをビルド </summary>
        public static AssetBundleManifest BuildAllAssetBundles(string exportPath)
        {
            var outputPath = GetAssetBundleOutputPath(exportPath);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            return BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.None, targetPlatform);
        }

        /// <summary> 情報書き込み後のAssetInfoManifestをビルド </summary>
        public static void BuildAssetInfoManifest(string exportPath, string externalResourcesPath)
        {
            var outputPath = GetAssetBundleOutputPath(exportPath);

            var manifestPath = PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);

            var buildMap = new AssetBundleBuild[]
            {
                new AssetBundleBuild()
                {
                    assetNames = new string[] { manifestPath },
                    assetBundleName = AssetInfoManifest.AssetBundleName,
                },
            };

            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            BuildPipeline.BuildAssetBundles(outputPath, buildMap, BuildAssetBundleOptions.None, targetPlatform);
        }

        /// <summary> アセットバンドル関連の不要なファイル削除 </summary>
        public static void DeleteUnUseFiles(string exportPath)
        {
            var deleteFiles = new List<string>();

            var allFiles = Directory.GetFiles(exportPath, "*.*", SearchOption.AllDirectories);

            // AssetBundleManifest.
            var assetBundleManifests = allFiles
                .Where(c => Path.GetFileNameWithoutExtension(c) == AssetBundleManifestName)
                .ToArray();

            deleteFiles.AddRange(assetBundleManifests);

            // ManifestFile.
            var manifestFiles = allFiles
                .Where(c => c.EndsWith(ManifestFileExtension))
                .ToArray();

            deleteFiles.AddRange(manifestFiles);

            // 削除.
            foreach (var deleteFile in deleteFiles)
            {
                File.Delete(deleteFile);
            }
        }

        /// <summary> アセットバンドルをパッケージ化 </summary>
        public static void BuildPackage(string exportPath)
        {
            var outputPath = GetAssetBundleOutputPath(exportPath);

            var allFiles = Directory.GetFiles(outputPath, "*.*", SearchOption.AllDirectories);

            var assetbundlePackageBuilder = new BuildAssetbundlePackage();

            Action<int, int> reportProgress = (current, total) =>
            {
                var title = "Build　AssetbundlePackage";
                var info = string.Format("Build progress ({0}/{1})", current, total);

                EditorUtility.DisplayProgressBar(title, info, current / (float)total);
            };

            assetbundlePackageBuilder.Build(allFiles, reportProgress);

            EditorUtility.ClearProgressBar();
        }
    }
}
