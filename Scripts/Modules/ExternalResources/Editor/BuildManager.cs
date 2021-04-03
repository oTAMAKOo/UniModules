﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.AssetBundles.Editor;
using Modules.Devkit.Console;
using Modules.Devkit.Prefs;

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

        public static async Task Build(string exportPath, AssetInfoManifest assetInfoManifest, bool openExportFolder = true)
        {
            if (string.IsNullOrEmpty(exportPath)) { return; }

            if (Directory.Exists(exportPath))
            {
                Directory.Delete(exportPath, true);
            }

            EditorApplication.LockReloadAssemblies();

            try
            {
                var logBuilder = new StringBuilder();

                var manageConfig = ManageConfig.Instance;

                using (new DisableStackTraceScope())
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    //------ キャッシュ済みアセットバンドルのハッシュ値取得 ------

                    var cachedAssetBundleHashTable = await BuildAssetBundle.GetCachedAssetBundleHash();

                    AddBuildTimeLog(logBuilder, sw, "GetCachedAssetBundleHash");

                    //------ CRIアセットを生成 ------

                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                    CriAssetGenerator.Generate(exportPath, assetInfoManifest);

                    AddBuildTimeLog(logBuilder, sw, "Generate CriAsset");

                    #endif

                    //------ AssetBundleをビルド ------

                    var assetBundleManifest = BuildAssetBundle.BuildAllAssetBundles();

                    BuildAssetBundle.CreateTemporarilyAssetBundleManifestFile();

                    AddBuildTimeLog(logBuilder, sw, "BuildAllAssetBundles");

                    //------ 不要になった古いAssetBundle削除 ------

                    BuildAssetBundle.CleanUnUseAssetBundleFiles();

                    AddBuildTimeLog(logBuilder, sw, "CleanUnUseAssetBundleFiles");

                    //------ ビルド成果物の情報をAssetInfoManifestに書き込み ------

                    var assetBundlePath = BuildAssetBundle.GetAssetBundleOutputPath();

                    AssetInfoManifestGenerator.SetAssetBundleFileInfo(assetBundlePath, assetBundleManifest);

                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                    AssetInfoManifestGenerator.SetCriAssetFileInfo(exportPath, assetBundleManifest);

                    #endif

                    AddBuildTimeLog(logBuilder, sw, "AssetInfoManifest : SetAssetBundleFileInfo");

                    //------ アセットバンドルの参照情報をAssetInfoManifestに書き込み ------

                    BuildAssetBundle.SetDependencies(assetInfoManifest, assetBundleManifest);

                    AddBuildTimeLog(logBuilder, sw, "AssetInfoManifest : SetAssetBundleDependencies");

                    //------ バージョンハッシュ情報をAssetInfoManifestに書き込み ------

                    BuildAssetBundle.SetAssetInfoHash(assetInfoManifest);

                    AddBuildTimeLog(logBuilder, sw, "AssetInfoManifest : SetAssetInfoHash");

                    //------ 再度AssetInfoManifestだけビルドを実行 ------

                    BuildAssetBundle.BuildAssetInfoManifest();

                    BuildAssetBundle.RestoreAssetBundleManifestFile();

                    AddBuildTimeLog(logBuilder, sw, "Rebuild AssetInfoManifest");

                    //------ 更新が必要なパッケージファイルを削除 ------

                    BuildAssetBundle.CleanOldPackage(cachedAssetBundleHashTable);

                    AddBuildTimeLog(logBuilder, sw, "CleanOldPackage");

                    //------ AssetBundleファイルをパッケージ化 ------

                    var cryptKey = manageConfig.CryptKey;
                    var cryptIv = manageConfig.CryptIv;

                    BuildAssetBundle.BuildPackage(exportPath, assetInfoManifest, cryptKey, cryptIv);

                    AddBuildTimeLog(logBuilder, sw, "BuildPackage");
                }

                //------ ログ出力------

                var logText = string.Format("Build ExternalResource Complete.\n\nVersionHash : {0}\n\n{1}", assetInfoManifest.VersionHash, logBuilder);

                UnityConsole.Event(ExternalResources.ConsoleEventName, ExternalResources.ConsoleEventColor, logText);

                //------ 出力先フォルダを開く------

                if (openExportFolder)
                {
                    UnityEditorUtility.OpenFolder(exportPath);
                }
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

        private static void AddBuildTimeLog(StringBuilder logBuilder, System.Diagnostics.Stopwatch sw, string text)
        {
            sw.Stop();

            logBuilder.AppendFormat("{0} : ({1:F1}sec)", text, sw.Elapsed.TotalSeconds).AppendLine();

            sw.Restart();
        }

        public static string SelectExportPath()
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
