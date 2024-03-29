
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;
using Modules.Devkit.Project;
using Modules.AssetBundles;
using Modules.AssetBundles.Editor;
using Modules.Devkit.Console;

namespace Modules.ExternalAssets
{
    public sealed class AssetInfoManifestGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static async UniTask<AssetInfoManifest> Generate()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            AssetInfoManifest manifest = null;

            var assetManagement = AssetManagement.Instance;

            var title = "Generate AssetInfoManifest";

            EditorUtility.DisplayProgressBar(title, "Setup Generate", 0.0f);

            assetManagement.Initialize();

            EditorUtility.DisplayProgressBar(title, "GetAllAssetInfos", 0.25f);

            var allAssetInfos = await assetManagement.GetAllAssetInfos();

            using (new AssetEditingScope())
            {
                EditorUtility.DisplayProgressBar(title, "DeleteUndefinedAssetBundleNames", 0.5f);

                DeleteUndefinedAssetBundleNames(assetManagement, allAssetInfos);

                EditorUtility.DisplayProgressBar(title, "GenerateManifestFile", 0.75f);

                manifest = GenerateManifest(allAssetInfos);

                EditorUtility.DisplayProgressBar(title, "ApplyAssetBundleName", 0.9f);

                ApplyAssetBundleName(assetManagement, manifest);
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();

            AssetManagement.Prefs.manifestUpdateRequest = false;

            EditorUtility.ClearProgressBar();

            sw.Stop(); 

            UnityConsole.Info($"Generate AssetInfoManifest ({sw.Elapsed.TotalMilliseconds:F2}ms)");

            return manifest;
        }

        /// <summary> 定義されていないアセットバンドル名を削除 </summary>
        public static void DeleteUndefinedAssetBundleNames(AssetManagement assetManagement, AssetInfo[] allAssetInfos)
        {
            var assetBundleNames = allAssetInfos
                .Where(x => x.IsAssetBundle && x.AssetBundle != null)
                .Select(x => x.AssetBundle.AssetBundleName)
                .Distinct()
                .ToHashSet();

            var allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();

            foreach (var assetBundleName in allAssetBundleNames)
            {
                if (assetBundleNames.Contains(assetBundleName)) { continue; }

                // AssetBundleNameをNoneに設定.
                var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);

                foreach (var assetPath in assetPaths)
                {
                    assetManagement.SetAssetBundleName(assetPath, string.Empty);
                }

                // AssetBundleNameを削除.
                AssetDatabase.RemoveAssetBundleName(assetBundleName, true);
            }
        }

        public static async UniTask SetAssetBundleFileInfo(string exportPath, AssetInfoManifest assetInfoManifest, BuildResult buildResult)
        {
            var assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");

            var assetBundleGroups = assetInfos
                .Where(x => x.IsAssetBundle)
                .GroupBy(x => x.AssetBundle.AssetBundleName);

            var tasks = new Dictionary<string, UniTask>();

            foreach (var assetBundleGroup in assetBundleGroups)
            {
                var assetBundleName = assetBundleGroup.Key;

                if (tasks.ContainsKey(assetBundleName)){ continue; }
                
                var assetInfo = assetBundleGroup.First();

                var detail = buildResult.GetDetails(assetBundleName);

                if (!detail.HasValue)
                {
                    throw new InvalidDataException("AssetBundle build info not found. : " + assetBundleName);
                }

                var filePath = PathUtility.Combine(exportPath, assetBundleName);

                // CRC.
                assetInfo.AssetBundle.SetCRC(detail.Value.Crc);

                // Hash.
                var assetBundleHash = detail.Value.Hash.ToString();

                // ファイルハッシュ・ファイルサイズ設定.

                var packageFilePath = filePath + AssetBundleManager.PackageExtension;

                if (!File.Exists(packageFilePath))
                {
                    throw new InvalidDataException("Package file not found. : " + packageFilePath);
                }

                var task = UniTask.RunOnThreadPool(() =>
                {
                    var fileInfo = new FileInfo(packageFilePath);

                    var size = fileInfo.Exists ? fileInfo.Length : -1;
                    var crc = FileUtility.GetCRC(packageFilePath);

                    // 同じアセットバンドル名の全アセット情報を更新.
                    foreach (var item in assetBundleGroup)
                    {
                        item.SetFileInfo(size, crc, assetBundleHash);
                    }
                });

                tasks.Add(assetBundleName, task);
            }

            await UniTask.WhenAll(tasks.Values);

            Reflection.SetPrivateField(assetInfoManifest, "assetInfos", assetInfos);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

            assetInfoManifest.BuildCache(true);

            EditorUtility.ClearProgressBar();
        }

        public static async UniTask SetFileAssetFileInfo(string exportPath, AssetInfoManifest assetInfoManifest)
        {
            var assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");
            
            var tasks = new List<UniTask>();

            for (var i = 0; i < assetInfos.Length; i++)
            {
                var assetInfo = assetInfos[i];

                if (assetInfo.IsAssetBundle) { continue; }
                
                var filePath = PathUtility.Combine(new string[] { exportPath, assetInfo.FileName });

                if (!File.Exists(filePath)) { continue; }

                var task = UniTask.RunOnThreadPool(() =>
                {
                    var fileInfo = new FileInfo(filePath);

                    var size = fileInfo.Exists ? fileInfo.Length : -1;
                    var crc = FileUtility.GetCRC(filePath);
                    var hash = FileUtility.GetHash(filePath);

                    assetInfo.SetFileInfo(size, crc, hash);
                });
                
                tasks.Add(task);
            }

            await UniTask.WhenAll(tasks);

            Reflection.SetPrivateField(assetInfoManifest, "assetInfos", assetInfos);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

            assetInfoManifest.BuildCache(true);

            EditorUtility.ClearProgressBar();
        }

        private static void ApplyAssetBundleName(AssetManagement assetManagement, AssetInfoManifest manifest)
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;
            var shareResourcesPath = projectResourceFolders.ShareResourcesPath;

            var assetInfos = manifest.GetAssetInfos().ToArray();

            var count = assetInfos.Length;

            for (var i = 0; i < count; i++)
            {
                var assetInfo = assetInfos[i];

                if (assetInfo.IsAssetBundle)
                {
                    var assetPath = ExternalAsset.GetAssetPathFromAssetInfo(externalAssetPath, shareResourcesPath, assetInfo);

                    assetManagement.SetAssetBundleName(assetPath, assetInfo.AssetBundle.AssetBundleName);
                }
            }
        }

        private static AssetInfoManifest GenerateManifest(AssetInfo[] allAssetInfos)
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;

            if (string.IsNullOrEmpty(externalAssetPath)){ return null; }

            // アセット情報を更新.
            var manifestPath = GetManifestPath(externalAssetPath);

            var manifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            if (manifest == null)
            {
                manifest = ScriptableObjectGenerator.Generate<AssetInfoManifest>(manifestPath);
            }
            else
            {
                manifest.Clear();
            }

            Reflection.SetPrivateField(manifest, "assetInfos", allAssetInfos);

            // 既に存在する場合は保存.

            if (AssetDatabase.IsMainAsset(manifest))
            {
                UnityEditorUtility.SaveAsset(manifest);
            }

            // アセットバンドル名設定.

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(manifest));

            importer.assetBundleName = AssetInfoManifest.AssetBundleName;
            importer.SaveAndReimport();

            return manifest;
        }

        public static string GetManifestPath(string externalAssetPath)
        {
            return PathUtility.Combine(externalAssetPath, AssetInfoManifest.ManifestFileName);
        }
    }
}
