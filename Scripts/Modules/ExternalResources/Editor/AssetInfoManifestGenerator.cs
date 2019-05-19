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
            var assetManageManager = AssetManageManager.Instance;

            assetManageManager.Initialize(externalResourcesPath, config);

            var manifest = GenerateManifest(assetManageManager);

            ApplyAssetBundleName(assetManageManager, manifest);

            UnityEditorUtility.SaveAsset(manifest);

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();
        }

        public static void SetAssetBundleFileInfo(string exportPath, string externalResourcesPath, AssetBundleManifest assetBundleManifest)
        {
            var manifestPath = GetManifestPath(externalResourcesPath);
            var assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            var progress = new ScheduledNotifier<Tuple<string,float>>();

            progress.Subscribe(prog => EditorUtility.DisplayProgressBar("Update assetbundle file info", prog.Item1, prog.Item2));

            assetInfoManifest.SetAssetBundleFileInfo(exportPath, progress);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

            EditorUtility.ClearProgressBar();
        }

        #if ENABLE_CRIWARE

        public static void SetCriAssetFileInfo(string exportPath, string externalResourcesPath, AssetBundleManifest assetBundleManifest)
        {
            var manifestPath = GetManifestPath(externalResourcesPath);
            var assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            var progress = new ScheduledNotifier<Tuple<string, float>>();

            progress.Subscribe(prog => EditorUtility.DisplayProgressBar("Update cri file info", prog.Item1, prog.Item2));

            assetInfoManifest.SetCriAssetFileInfo(exportPath, progress);

            UnityEditorUtility.SaveAsset(assetInfoManifest);

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

                EditorUtility.DisplayProgressBar("ApplyAssetBundleName", assetInfo.ResourcesPath, (float)i / count);

                var assetPath = PathUtility.Combine(assetManageManager.ExternalResourcesPath, assetInfo.ResourcesPath);

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
