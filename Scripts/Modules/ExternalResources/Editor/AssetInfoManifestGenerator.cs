﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;
using Modules.Devkit.Project;
using Modules.AssetBundles;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare;

#endif

namespace Modules.ExternalResource.Editor
{
    public sealed class AssetInfoManifestGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static AssetInfoManifest Generate()
        {
            AssetInfoManifest manifest = null;

            var assetManagement = AssetManagement.Instance;

            assetManagement.Initialize();

            using (new AssetEditingScope())
            {
                DeleteUndefinedAssetBundleNames(assetManagement);

                manifest = GenerateManifest(assetManagement);

                ApplyAssetBundleName(assetManagement, manifest);
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();

            AssetManagement.Prefs.manifestUpdateRequest = false;

            return manifest;
        }

        /// <summary> 定義されていないアセットバンドル名を削除 </summary>
        public static void DeleteUndefinedAssetBundleNames(AssetManagement assetManagement)
        {
            var assetBundleNames = assetManagement.GetAllAssetInfos()
                .Where(x => x.IsAssetBundle && x.AssetBundle != null)
                .Select(x => x.AssetBundle.AssetBundleName)
                .Distinct()
                .ToHashSet();

            var allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();

            foreach (var assetBundleName in allAssetBundleNames)
            {
                if (assetBundleNames.Contains(assetBundleName)) { continue; }

                AssetDatabase.RemoveAssetBundleName(assetBundleName, true);
            }
        }

        public static async Task SetAssetBundleFileInfo(string exportPath, AssetInfoManifest assetInfoManifest)
        {
            var assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");

            var crcCache = new Dictionary<string, uint>();
            var fileInfoCache = new Dictionary<string, Tuple<long, string>>();

            var tasks = new List<Task>();
            
            for (var i = 0; i < assetInfos.Length; i++)
            {
                var assetInfo = assetInfos[i];

                if (!assetInfo.IsAssetBundle) { continue; }

                var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                var filePath = PathUtility.Combine(exportPath, assetBundleName);

                // CRC設定.

                uint crc = 0;

                if (crcCache.ContainsKey(filePath))
                {
                    crc = crcCache[filePath];
                }
                else
                {
                    BuildPipeline.GetCRCForAssetBundle(filePath, out crc);

                    crcCache[filePath] = crc;
                }

                assetInfo.AssetBundle.SetCRC(crc);

                // ファイルハッシュ・ファイルサイズ設定.

                var packageFilePath = Path.ChangeExtension(filePath, AssetBundleManager.PackageExtension);

                var task = Task.Run(() =>
                {
                    SetFileInfo(assetInfo, fileInfoCache, packageFilePath);
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            Reflection.SetPrivateField(assetInfoManifest, "assetInfos", assetInfos);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

            assetInfoManifest.BuildCache(true);

            EditorUtility.ClearProgressBar();
        }

        private static void ApplyAssetBundleName(AssetManagement assetManagement, AssetInfoManifest manifest)
        {
            var projectFolders = ProjectFolders.Instance;

            var externalResourcesPath = projectFolders.ExternalResourcesPath;
            var shareResourcesPath = projectFolders.ShareResourcesPath;

            var assetInfos = manifest.GetAssetInfos().ToArray();

            var count = assetInfos.Length;

            using (new AssetEditingScope())
            {
                for (var i = 0; i < count; i++)
                {
                    var assetInfo = assetInfos[i];

                    var apply = false;

                    if (assetInfo.IsAssetBundle)
                    {
                        var assetPath = ExternalResources.GetAssetPathFromAssetInfo(externalResourcesPath, shareResourcesPath, assetInfo);

                        apply = assetManagement.SetAssetBundleName(assetPath, assetInfo.AssetBundle.AssetBundleName);
                    }

                    if (apply)
                    {
                        EditorUtility.DisplayProgressBar("ApplyAssetBundleName", assetInfo.ResourcePath, (float)i / count);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private static AssetInfoManifest GenerateManifest(AssetManagement assetManagement)
        {
            var projectFolders = ProjectFolders.Instance;

            var externalResourcesPath = projectFolders.ExternalResourcesPath;

            var allAssetInfos = assetManagement.GetAllAssetInfos().ToArray();

            // アセット情報を更新.
            var manifestPath = GetManifestPath(externalResourcesPath);
            var manifest = ScriptableObjectGenerator.Generate<AssetInfoManifest>(manifestPath);

            Reflection.SetPrivateField(manifest, "assetInfos", allAssetInfos);

            UnityEditorUtility.SaveAsset(manifest);

            // アセットバンドル名設定.
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(manifest));

            importer.assetBundleName = AssetInfoManifest.AssetBundleName;
            importer.SaveAndReimport();

            return manifest;
        }

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

        public static async Task SetCriAssetFileInfo(string exportPath, AssetInfoManifest assetInfoManifest)
        {
            var assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");
            
            var tasks = new List<Task>();

            for (var i = 0; i < assetInfos.Length; i++)
            {
                var assetInfo = assetInfos[i];

                if (assetInfo.IsAssetBundle) { continue; }

                var extension = Path.GetExtension(assetInfo.FileName);

                if (CriAssetDefinition.AssetAllExtensions.Any(x => x == extension))
                {
                    var filePath = PathUtility.Combine(new string[] { exportPath, assetInfo.FileName });

                    var task = Task.Run(() =>
                    {
                        SetFileInfo(assetInfo, null, filePath);
                    });
                    
                    tasks.Add(task);
                }
            }

            await Task.WhenAll(tasks);

            Reflection.SetPrivateField(assetInfoManifest, "assetInfos", assetInfos);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

            assetInfoManifest.BuildCache(true);

            EditorUtility.ClearProgressBar();
        }

        #endif

        private static void SetFileInfo(AssetInfo assetInfo, Dictionary<string, Tuple<long, string>> fileInfoCache, string filePath)
        {
            if (!File.Exists(filePath)) { return; }

            Tuple<long, string> info = null;

            long fileSize = 0;
            string fileHash = null;

            if (fileInfoCache != null)
            {
                lock (fileInfoCache)
                {
                    info = fileInfoCache.GetValueOrDefault(filePath);
                }
            }

            if (info != null)
            {
                fileSize = info.Item1;
                fileHash = info.Item2;
            }
            else
            {
                var fileInfo = new FileInfo(filePath);

                fileSize = fileInfo.Exists ? fileInfo.Length : -1;
                fileHash = FileUtility.GetHash(filePath);

                if (fileInfoCache != null)
                {
                    lock (fileInfoCache)
                    {
                        fileInfoCache[filePath] = Tuple.Create(fileSize, fileHash);
                    }
                }
            }

            assetInfo.SetFileInfo(fileSize, fileHash);
        }

        private static string GetManifestPath(string externalResourcesPath)
        {
            return PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);
        }
    }
}
