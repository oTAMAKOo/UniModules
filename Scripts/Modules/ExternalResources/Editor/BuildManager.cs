﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Extensions;
using Extensions.Devkit;
using Modules.AssetBundles;
using Modules.AssetBundles.Editor;
using Modules.Devkit;
using Modules.Devkit.Prefs;
using Modules.Devkit.Project;
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare.Editor;

#endif

namespace Modules.ExternalResource.Editor
{
    public static class BuildManager
    {
        //----- params -----

        private const string ExportFolderName = "ExternalResources";

        private static class Prefs
        {
            public static string exportPath
            {
                get { return ProjectPrefs.GetString("ExternalResourceManager-Prefs-exportPath", UnityPathUtility.GetProjectFolderPath()); }
                set { ProjectPrefs.SetString("ExternalResourceManager-Prefs-exportPath", value); }
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool BuildConfirm()
        {
            return EditorUtility.DisplayDialog("Confirmation", "外部アセットを生成します.", "実行", "中止");
        }

        public static void Build()
        {
            var exportPath = GetExportPath();

            if (string.IsNullOrEmpty(exportPath)) { return; }

            if (Directory.Exists(exportPath))
            {
                Directory.Delete(exportPath, true);
            }

            EditorApplication.LockReloadAssemblies();

            try
            {
                var manageConfig = ManageConfig.Instance;

                // アセット情報ファイルを生成.
                var assetInfoManifest = AssetInfoManifestGenerator.Generate();

                // キャッシュ済みアセットバンドルのハッシュ値取得.
                var cachedAssetBundleHashs = BuildAssetBundle.GetCachedAssetBundleHash();

                // CRIアセットを生成.
                #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                CriAssetGenerator.Generate(exportPath, assetInfoManifest);

                #endif
                
                // AssetBundleをビルド.
                var assetBundleManifest = BuildAssetBundle.BuildAllAssetBundles();

                // 不要になった古いAssetBundle削除.
                BuildAssetBundle.CleanUnUseAssetBundleFiles();

                // ビルド成果物の情報をAssetInfoManifestに書き込み.

                var assetBundlePath = BuildAssetBundle.GetAssetBundleOutputPath();

                AssetInfoManifestGenerator.SetAssetBundleFileInfo(assetBundlePath, assetBundleManifest);

                #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                AssetInfoManifestGenerator.SetCriAssetFileInfo(exportPath, assetBundleManifest);

                #endif

                // アセットバンドルの参照情報をAssetInfoManifestに書き込み.
                BuildAssetBundle.SetDependencies(assetInfoManifest, assetBundleManifest);

                // 再度AssetInfoManifestだけビルドを実行.
                BuildAssetBundle.BuildAssetInfoManifest();

                // 更新が必要なパッケージファイルを削除.
                BuildAssetBundle.CleanOldPackage(cachedAssetBundleHashs);

                // AssetBundleファイルをパッケージ化.
                BuildAssetBundle.BuildPackage(exportPath, assetInfoManifest, manageConfig.CryptPassword);

                // 出力先フォルダを開く.
                UnityEditorUtility.OpenFolder(exportPath);

                UnityConsole.Event(ExternalResources.ConsoleEventName, ExternalResources.ConsoleEventColor, "Build ExternalResource Complete.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }
        
        private static string GetExportPath()
        {
            var directory = string.IsNullOrEmpty(Prefs.exportPath) ? null : Path.GetDirectoryName(Prefs.exportPath);
            var folderName = string.IsNullOrEmpty(Prefs.exportPath) ? ExportFolderName : Path.GetFileName(Prefs.exportPath);

            var path = EditorUtility.OpenFolderPanel("Select export folder.", directory, folderName);

            if (string.IsNullOrEmpty(path)) { return null; }    

            Prefs.exportPath = path;

            var platformAssetFolderName = PlatformUtility.GetPlatformAssetFolderName();

            return PathUtility.Combine(path, platformAssetFolderName) + PathUtility.PathSeparator;
        }        
    }
}
