﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare;

#endif

namespace Modules.ExternalResource.Editor
{
    public class AssetInfoManifestGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static AssetInfoManifest Generate(string externalResourcesPath, AssetManageConfig config)
        {
            var assetManageManager = AssetManageManager.Instance;

            assetManageManager.Initialize(externalResourcesPath, config);

            var manifest = GenerateManifest(assetManageManager);

            ApplyAssetBundleName(assetManageManager, manifest);

            UnityEditorUtility.SaveAsset(manifest);

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();

            return manifest;
        }

        public static void SetAssetBundleFileInfo(string exportPath, string externalResourcesPath, AssetBundleManifest assetBundleManifest)
        {
            var manifestPath = GetManifestPath(externalResourcesPath);
            var assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            var progress = new ScheduledNotifier<Tuple<string,float>>();

            progress.Subscribe(prog => EditorUtility.DisplayProgressBar("Update assetbundle file info", prog.Item1, prog.Item2));

            var assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");
            
            for (var i = 0; i < assetInfos.Length; i++)
            {
                var assetInfo = assetInfos[i];

                if (!assetInfo.IsAssetBundle) { continue; }

                var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                var filePath = PathUtility.Combine(new string[] { exportPath, assetBundleName });

                BuildPipeline.GetCRCForAssetBundle(filePath, out var crc);

                var hashSource = string.Format("{0}-{1}", assetBundleName, crc);

                var assetBundleHash = hashSource.GetHash();

                assetInfo.SetFileInfo(filePath, assetBundleHash);

                progress.Report(Tuple.Create(assetInfo.ResourcePath, (float)i / assetInfos.Length));
            }

            Reflection.SetPrivateField(assetInfoManifest, "assetInfos", assetInfos);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

            assetInfoManifest.BuildCache(true);

            EditorUtility.ClearProgressBar();
        }

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

        public static void SetCriAssetFileInfo(string exportPath, string externalResourcesPath, AssetBundleManifest assetBundleManifest)
        {
            var manifestPath = GetManifestPath(externalResourcesPath);
            var assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            var progress = new ScheduledNotifier<Tuple<string, float>>();

            progress.Subscribe(prog => EditorUtility.DisplayProgressBar("Update cri file info", prog.Item1, prog.Item2));

            var assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(assetInfoManifest, "assetInfos");

            for (var i = 0; i < assetInfos.Length; i++)
            {
                var assetInfo = assetInfos[i];

                if (assetInfo.IsAssetBundle) { continue; }

                var extension = Path.GetExtension(assetInfo.FileName);

                var filePath = string.Empty;

                if (CriAssetDefinition.AssetAllExtensions.Any(x => x == extension))
                {
                    filePath = PathUtility.Combine(new string[] { exportPath, assetInfo.FileName });
                }

                var fileHash = FileUtility.GetHash(filePath);

                assetInfo.SetFileInfo(filePath, fileHash);

                progress.Report(Tuple.Create(assetInfo.ResourcePath, (float)i / assetInfos.Length));
            }

            Reflection.SetPrivateField(assetInfoManifest, "assetInfos", assetInfos);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

            assetInfoManifest.BuildCache(true);

            EditorUtility.ClearProgressBar();
        }

        #endif

        private static void ApplyAssetBundleName(AssetManageManager assetManageManager, AssetInfoManifest manifest)
        {
            var assetInfos = manifest.GetAssetInfos().ToArray();

            var count = assetInfos.Length;

            AssetDatabase.StartAssetEditing();

            for (var i = 0; i < count; i++)
            {
                var assetInfo = assetInfos[i];

                EditorUtility.DisplayProgressBar("ApplyAssetBundleName", assetInfo.ResourcePath, (float)i / count);

                var assetPath = PathUtility.Combine(assetManageManager.ExternalResourcesPath, assetInfo.ResourcePath);

                if (assetInfo.IsAssetBundle)
                {
                    assetManageManager.SetAssetBundleName(assetPath, assetInfo.AssetBundle.AssetBundleName);
                }
            }

            AssetDatabase.StopAssetEditing();

            EditorUtility.ClearProgressBar();
        }

        private static AssetInfoManifest GenerateManifest(AssetManageManager assetManageManager)
        {
            // アセット情報を収集.
            assetManageManager.CollectInfo();

            var allAssetInfos = assetManageManager.GetAllAssetInfos().ToArray();

            // アセット情報を更新.
            var manifestPath = GetManifestPath(assetManageManager.ExternalResourcesPath);
            var manifest = ScriptableObjectGenerator.Generate<AssetInfoManifest>(manifestPath);

            Reflection.SetPrivateField(manifest, "assetInfos", allAssetInfos);

            UnityEditorUtility.SaveAsset(manifest);

            // アセットバンドル名設定.
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(manifest));
            importer.assetBundleName = AssetInfoManifest.AssetBundleName;
            importer.SaveAndReimport();

            return manifest;
        }

        private static string GetManifestPath(string externalResourcesPath)
        {
            return PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);
        }
    }
}
