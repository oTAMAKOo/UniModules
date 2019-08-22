
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

            var assetBundlesFolder = AssetBundleManager.AssetBundlesFolder;

            var assetBundlePath = PathUtility.Combine(new string[] { projectPath, AssetBundleManager.LibraryFolder, assetBundlesFolder, platformName });

            if (!Directory.Exists(assetBundlePath))
            {
                Directory.CreateDirectory(assetBundlePath);
            }

            return assetBundlePath;
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

            // 必ず更新するのでパッケージファイルを削除.            
            var manifestFullPath = PathUtility.Combine(assetBundlePath, AssetInfoManifest.ManifestFileName);
            var manifestFilePackage = Path.ChangeExtension(manifestFullPath, AssetBundleManager.PackageExtension);

            if (File.Exists(manifestFilePackage))
            {
                File.Delete(manifestFilePackage);
            }

            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            BuildPipeline.BuildAssetBundles(assetBundlePath, buildMap, BuildAssetBundleOptions.ChunkBasedCompression, targetPlatform);
        }

        /// <summary> アセットバンドルをパッケージ化 </summary>
        public static void BuildPackage(string exportPath, string password)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetbundlePackageBuilder = new BuildAssetbundlePackage();

            Action<int, int> reportProgress = (current, total) =>
            {
                var title = "Build　AssetbundlePackage";
                var info = string.Format("Build progress ({0}/{1})", current, total);

                EditorUtility.DisplayProgressBar(title, info, current / (float)total);
            };

            assetbundlePackageBuilder.Build(exportPath, assetBundlePath, password, reportProgress);

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
            
            foreach (var file in allFiles)
            {
                var extension = Path.GetExtension(file);

                if (extension == ManifestFileExtension) { continue; }

                if (extension == AssetBundleManager.PackageExtension) { continue; }

                var path = PathUtility.ConvertPathSeparator(file);

                if (assetBundleNames.Any(x => path.EndsWith(x))){ continue; }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                var manifestFilePath = Path.ChangeExtension(path, ManifestFileExtension);

                if (File.Exists(manifestFilePath))
                {
                    File.Delete(manifestFilePath);
                }                
            }

            // 空フォルダ削除.
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                DirectoryUtility.DeleteEmpty(assetBundlePath);
            }
        }

        /// <summary> キャッシュ済みアセットバンドルファイルのハッシュ値を取得 </summary>
        public static Dictionary<string, string> GetCachedAssetBundleHash()
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetBundleNames = AssetDatabase.GetAllAssetBundleNames()
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories);

            var dictionary = new Dictionary<string, string>();

            foreach (var file in allFiles)
            {
                var path = PathUtility.ConvertPathSeparator(file);

                if (assetBundleNames.All(x => !path.EndsWith(x))) { continue; }

                var hash = FileUtility.GetHash(path);

                dictionary.Add(path, hash);
            }

            return dictionary;
        }

        /// <summary> 更新が必要なパッケージファイルを削除 </summary>
        public static void CleanOldPackage(Dictionary<string, string> cachedAssetBundleHashs)
        {
            foreach (var item in cachedAssetBundleHashs)
            {
                var file = item.Key;

                var hash = FileUtility.GetHash(file);
                
                if (hash == item.Value) { continue; }

                var packageFile = Path.ChangeExtension(file, AssetBundleManager.PackageExtension);

                if (File.Exists(packageFile))
                {
                    File.Delete(packageFile);
                }
            }
        }
    }
}
