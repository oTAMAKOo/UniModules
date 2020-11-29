
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions;
using Modules.Devkit.Project;
using Modules.ExternalResource;

namespace Modules.AssetBundles.Editor
{
    public static class BuildAssetBundle
    {
        //----- params -----

        private const string ManifestFileExtension = ".manifest";

        private const string AssetBundleCacheFolder = "AssetBundleBuildCache";

        //----- field -----

        //----- property -----

        //----- method -----

        public static string GetAssetBundleOutputPath()
        {
            var projectPath = UnityPathUtility.GetProjectFolderPath();
            var folderName = PlatformUtility.GetPlatformAssetFolderName();

            var paths = new string[] { projectPath, UnityPathUtility.LibraryFolder, AssetBundleCacheFolder, folderName };

            var assetBundlePath = PathUtility.Combine(paths);

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

        /// <summary> アセットバンドルの参照情報を書き込み </summary>
        public static void SetDependencies(AssetInfoManifest assetInfoManifest, AssetBundleManifest assetBundleManifest)
        {
            var assetInfos = assetInfoManifest.GetAssetInfos().Where(x => x.IsAssetBundle).ToArray();

            foreach (var assetInfo in assetInfos)
            {
                var dependencies = assetBundleManifest.GetDirectDependencies(assetInfo.AssetBundle.AssetBundleName);

                assetInfo.AssetBundle.SetDependencies(dependencies);
            }
        }

        /// <summary> 情報書き込み後のAssetInfoManifestをビルド </summary>
        public static void BuildAssetInfoManifest()
        {
            var projectFolders = ProjectFolders.Instance;

            if (projectFolders == null){ return; }

            var externalResourcesPath = projectFolders.ExternalResourcesPath;

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
        public static void BuildPackage(string exportPath, AssetInfoManifest assetInfoManifest, string password)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetBundlePackageBuilder = new BuildAssetbundlePackage();

            Action<int, int> reportProgress = (current, total) =>
            {
                var title = "Build　AssetBundle Package";
                var info = string.Format("Build progress ({0}/{1})", current, total);

                EditorUtility.DisplayProgressBar(title, info, current / (float)total);
            };

            assetBundlePackageBuilder.Build(exportPath, assetBundlePath, assetInfoManifest, password, reportProgress);

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
