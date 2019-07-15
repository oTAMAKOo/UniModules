
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

        private const string ManifestFileExtension = ".manifest";

        //----- field -----

        //----- property -----

        //----- method -----

        public static string GetAssetBundleOutputPath()
        {
            var projectPath = UnityPathUtility.GetProjectFolderPath();
            var platformName = UnityPathUtility.GetPlatformName();

            return PathUtility.Combine(new string[] { projectPath, AssetBundleManager.LibraryFolder, AssetBundleManager.AssetBundlesFolder, platformName });
        }

        /// <summary> 全てのアセットバンドルをビルド </summary>
        public static AssetBundleManifest BuildAllAssetBundles()
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            if (!Directory.Exists(assetBundlePath))
            {
                Directory.CreateDirectory(assetBundlePath);
            }

            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            return BuildPipeline.BuildAssetBundles(assetBundlePath, BuildAssetBundleOptions.ChunkBasedCompression, targetPlatform);
        }

        /// <summary> 情報書き込み後のAssetInfoManifestをビルド </summary>
        public static void BuildAssetInfoManifest(string externalResourcesPath)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

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

            BuildPipeline.BuildAssetBundles(assetBundlePath, buildMap, BuildAssetBundleOptions.ChunkBasedCompression, targetPlatform);
        }

        /// <summary> アセットバンドルをパッケージ化 </summary>
        public static void BuildPackage(string exportPath)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetbundlePackageBuilder = new BuildAssetbundlePackage();

            Action<int, int> reportProgress = (current, total) =>
            {
                var title = "Build　AssetbundlePackage";
                var info = string.Format("Build progress ({0}/{1})", current, total);

                EditorUtility.DisplayProgressBar(title, info, current / (float)total);
            };

            assetbundlePackageBuilder.Build(exportPath, assetBundlePath, reportProgress);

            EditorUtility.ClearProgressBar();
        }

        /// <summary> 不要になったアセットバンドルを削除 </summary>
        public static void CleanUnUseAssetBundleFiles(DateTime buildTime)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories);

            var deleteFiles = allFiles
                .Where(x => Path.GetExtension(x) == ManifestFileExtension)
                .Where(x => File.GetLastAccessTimeUtc(x) < buildTime);

            foreach (var file in deleteFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                var assetBundleFile = Path.ChangeExtension(file, string.Empty);

                if (File.Exists(assetBundleFile))
                {
                    File.Delete(assetBundleFile);
                }
            }

            // 空フォルダ削除.
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                DirectoryUtility.DeleteEmpty(assetBundlePath);
            }
        }
    }
}
