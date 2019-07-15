
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

            return BuildPipeline.BuildAssetBundles(assetBundlePath, BuildAssetBundleOptions.None, targetPlatform);
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

            BuildPipeline.BuildAssetBundles(assetBundlePath, buildMap, BuildAssetBundleOptions.None, targetPlatform);
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
        public static void CleanUnUseAssetBundleFiles()
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetBundleNames = AssetDatabase.GetAllAssetBundleNames()
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories);

            // アセットバンドル名一覧にない物は
            var deleteFiles = allFiles
                .Where(x => Path.GetExtension(x) != ManifestFileExtension)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .Where(x => assetBundleNames.All(y => !x.EndsWith(y)))
                .ToArray();
            
            foreach (var file in deleteFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Debug.Log("Delete file: " + file);
                }

                var manifestFile = Path.ChangeExtension(file, ManifestFileExtension);

                if (File.Exists(manifestFile))
                {
                    File.Delete(manifestFile);
                    Debug.Log("Delete manifestFile: " + manifestFile);
                }                
            }

            // 空フォルダ削除.
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                DirectoryUtility.DeleteEmpty(assetBundlePath);
            }
        }

        /// <summary> 更新が必要なパッケージファイルを削除 </summary>
        public static void DeleteCache(Dictionary<string, string> assetBundleFileHashs)
        {
            var packageFile = Path.ChangeExtension(file, AssetBundleManager.PackageExtension);

            if (File.Exists(packageFile))
            {
                File.Delete(packageFile);
                Debug.Log("Delete packageFile: " + packageFile);
            }
        }
    }
}
