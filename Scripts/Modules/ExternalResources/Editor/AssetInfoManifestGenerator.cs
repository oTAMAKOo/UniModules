﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;

namespace Modules.ExternalResource.Editor
{
    public class AssetInfoManifestGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(string externalResourcesPath, AssetManageConfig config)
        {
            var assetManageManager = new AssetManageManager();

            assetManageManager.Initialize(externalResourcesPath, config);

            var manifest = GenerateManifest(assetManageManager);

            ApplyAssetBundleName(assetManageManager, manifest);

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }

        public static void SetAssetFileInfo(string exportPath, string externalResourcesPath)
        {
            var manifestPath = PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);
            var assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            var progress = new ScheduledNotifier<Tuple<string,float>>();

            progress.Subscribe(prog => EditorUtility.DisplayProgressBar("SetAssetFileInfo", prog.Item1, prog.Item2));

            assetInfoManifest.SetAssetFileInfo(exportPath, progress);

            UnityEditorUtility.SaveAsset(assetInfoManifest);
        }

        private static void ApplyAssetBundleName(AssetManageManager assetManageManager, AssetInfoManifest manifest)
        {
            var assetInfos = manifest.GetAssetInfos().ToArray();

            var count = assetInfos.Length;

            AssetDatabase.StartAssetEditing();

            for (var i = 0; i < count; i++)
            {
                var assetInfo = assetInfos[i];

                EditorUtility.DisplayProgressBar("ApplyAssetBundleName", assetInfo.ResourcesPath, (float)i / count);

                var assetPath = PathUtility.Combine(assetManageManager.ExternalResourcesPath, assetInfo.ResourcesPath);

                assetManageManager.SetAssetBundleName(assetPath, assetInfo.AssetBundleName);
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
            var manifestPath = PathUtility.Combine(assetManageManager.ExternalResourcesPath, AssetInfoManifest.ManifestFileName);
            var manifest = ScriptableObjectGenerator.Generate<AssetInfoManifest>(manifestPath);

            Reflection.SetPrivateField(manifest, "assetInfos", allAssetInfos);

            UnityEditorUtility.SaveAsset(manifest);

            // アセットバンドル名設定.
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(manifest));
            importer.assetBundleName = AssetInfoManifest.AssetBundleName;
            importer.SaveAndReimport();

            return manifest;
        }
    }
}