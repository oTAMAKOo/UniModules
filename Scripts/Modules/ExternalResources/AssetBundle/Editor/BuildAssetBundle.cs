
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using Modules.Devkit.Project;
using Modules.ExternalResource;

namespace Modules.AssetBundles.Editor
{
    public static class BuildAssetBundle
    {
        //----- params -----

        private const string ManifestFileExtension = ".manifest";

        private const string ManifestTemporarilyFileExtension = ".buidtemp";

        private const string AssetBundleCacheFolder = "AssetBundleBuildCache";

        //----- field -----

        //----- property -----

        //----- method -----

        public static string GetAssetBundleOutputPath()
        {
            var projectPath = UnityPathUtility.GetProjectFolderPath();
            var folderName = PlatformUtility.GetPlatformTypeName();

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

        /// <summary> アセットのルートハッシュ情報を書き込み </summary>
        public static void SetAssetInfoHash(AssetInfoManifest assetInfoManifest)
        {
            // 文字数が大きくなりすぎないように300ファイル分毎に分割.
            var chunkInfos = assetInfoManifest.GetAssetInfos()
                .OrderBy(x => x.ResourcePath.Length)
                .Chunk(300)
                .ToArray();

            var versionHashBuilder = new StringBuilder();

            foreach (var assetInfos in chunkInfos)
            {
                var hashBuilder = new StringBuilder();

                foreach (var assetInfo in assetInfos)
                {
                    hashBuilder.AppendLine(assetInfo.FileHash);
                }

                var hash = hashBuilder.ToString().GetHash();

                versionHashBuilder.AppendLine(hash);
            }

            var versionHash = versionHashBuilder.ToString().GetHash();

            Reflection.SetPrivateField(assetInfoManifest, "versionHash", versionHash);
        }

        /// <summary> 情報書き込み後のAssetInfoManifestをビルド </summary>
        public static void BuildAssetInfoManifest()
        {
            var projectFolders = ProjectFolders.Instance;

            if (projectFolders == null) { return; }

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

        /// <summary> 現在のアセットバンドルマニフェストを一時退避 </summary>
        public static void CreateTemporarilyAssetBundleManifestFile()
        {
            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            var manifestFilePath = GetAssetBundleManifestFilePath(targetPlatform);

            var main = Path.ChangeExtension(manifestFilePath, string.Empty);
            var manifest = Path.ChangeExtension(manifestFilePath, ManifestFileExtension);

            File.Copy(main, main + ManifestTemporarilyFileExtension, true);
            File.Copy(manifest, manifest + ManifestTemporarilyFileExtension, true);
        }

        /// <summary> 一時退避したアセットバンドルマニフェストを復元 </summary>
        public static void RestoreAssetBundleManifestFile()
        {
            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            var manifestFilePath = GetAssetBundleManifestFilePath(targetPlatform);

            var main = Path.ChangeExtension(manifestFilePath, string.Empty);
            var manifest = Path.ChangeExtension(manifestFilePath, ManifestFileExtension);

            if (File.Exists(main))
            {
                File.Delete(main);
            }

            File.Move(main + ManifestTemporarilyFileExtension, main);

            if (File.Exists(manifest))
            {
                File.Delete(manifest);
            }

            File.Move(manifest + ManifestTemporarilyFileExtension, manifest);
        }

        /// <summary> アセットバンドルマニフェストのファイルパス取得 </summary>
        private static string GetAssetBundleManifestFilePath(BuildTarget targetPlatform)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            return PathUtility.Combine(assetBundlePath, targetPlatform.ToString());
        }

        /// <summary> 不要になったアセットバンドルを削除 </summary>
        public static void CleanUnUseAssetBundleFiles()
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetBundleNames = AssetDatabase.GetAllAssetBundleNames()
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            foreach (var file in allFiles)
            {
                var extension = Path.GetExtension(file);

                if (extension == ManifestFileExtension) { continue; }

                if (extension == ManifestTemporarilyFileExtension) { continue; }

                if (extension == AssetBundleManager.PackageExtension) { continue; }

                if (assetBundleNames.Any(x => file.EndsWith(x))) { continue; }

                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                var manifestFilePath = Path.ChangeExtension(file, ManifestFileExtension);

                if (File.Exists(manifestFilePath))
                {
                    File.Delete(manifestFilePath);
                }

                var packageFilePath = Path.ChangeExtension(file, AssetBundleManager.PackageExtension);

                if (File.Exists(packageFilePath))
                {
                    File.Delete(packageFilePath);
                }
            }

            // 空フォルダ削除.
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                DirectoryUtility.DeleteEmpty(assetBundlePath);
            }
        }

        /// <summary> キャッシュ済みアセットバンドルファイルの最終更新日テーブルを取得 </summary>
        public static async Task<Dictionary<string, DateTime>> GetCachedFileLastWriteTimeTable()
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetBundleNames = AssetDatabase.GetAllAssetBundleNames()
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var allFiles = Directory.GetFiles(assetBundlePath, "*.*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) == string.Empty)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var dictionary = new Dictionary<string, DateTime>();

            var tasks = new List<Task>();

            foreach (var file in allFiles)
            {
                var path = PathUtility.ConvertPathSeparator(file);

                var task = Task.Run(() =>
                {
                    if (assetBundleNames.All(x => !path.EndsWith(x))) { return; }

                    var lastWriteTime = File.GetLastWriteTime(path);

                    lock (dictionary)
                    {
                        dictionary.Add(path, lastWriteTime);
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            return dictionary;
        }

        /// <summary> 更新されたアセット情報取得 </summary>
        public static async Task<AssetInfo[]> GetUpdateTargetAssetInfo(AssetInfoManifest assetInfoManifest, Dictionary<string, DateTime> lastWriteTimeTable)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            var assetInfos = assetInfoManifest.GetAssetInfos()
                .Where(x => x.IsAssetBundle)
                .Where(x => !string.IsNullOrEmpty(x.AssetBundle.AssetBundleName))
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToList();

            assetInfos.Add(AssetInfoManifest.GetManifestAssetInfo());

            var list = new List<AssetInfo>();

            var tasks = new List<Task>();

            foreach (var item in assetInfos)
            {
                var assetInfo = item;

                var task = Task.Run(() =>
                {
                    // アセットバンドルファイルパス.
                    var assetBundleFilePath = PathUtility.Combine(assetBundlePath, assetInfo.AssetBundle.AssetBundleName);
                    
                    // 最終更新日を比較.

                    var prevLastWriteTime = lastWriteTimeTable.GetValueOrDefault(assetBundleFilePath, DateTime.MinValue);

                    var currentLastWriteTime = File.GetLastWriteTime(assetBundleFilePath);

                    // ビルド前と後で更新日時が変わっていたら更新対象.
                    if (currentLastWriteTime != prevLastWriteTime)
                    {
                        list.Add(assetInfo);
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            return list.ToArray();
        }

        /// <summary> 更新予定のパッケージファイルを削除 </summary>
        public static void DeleteUpdateTargetPackage(AssetInfo[] assetInfos)
        {
            var assetBundlePath = GetAssetBundleOutputPath();

            foreach (var assetInfo in assetInfos)
            {
                // アセットバンドルファイルパス.
                var assetBundleFilePath = PathUtility.Combine(assetBundlePath, assetInfo.AssetBundle.AssetBundleName);

                // パッケージファイルパス.
                var packageFilePath = Path.ChangeExtension(assetBundleFilePath, AssetBundleManager.PackageExtension);

                if (File.Exists(packageFilePath))
                {
                    File.Delete(packageFilePath);
                }
            }
        }
    }
}
