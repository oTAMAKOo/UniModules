﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
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

        public static async Task<string> Build(string exportPath, AssetInfoManifest assetInfoManifest, bool openExportFolder = true)
        {
            if (string.IsNullOrEmpty(exportPath)) { return null; }

            if (Directory.Exists(exportPath))
            {
                Directory.Delete(exportPath, true);
            }

            var versionHash = string.Empty;

            var assetManagement = AssetManagement.Instance;

            assetManagement.Initialize();

            EditorApplication.LockReloadAssemblies();

            try
            {
                var logBuilder = new StringBuilder();

                var manageConfig = ManageConfig.Instance;

                using (new DisableStackTraceScope())
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    //------ アセットバンドル名を設定------

                    assetManagement.ApplyAllAssetBundleName();

                    AddBuildTimeLog(logBuilder, sw, "ApplyAllAssetBundleName");

                    //------ キャッシュ済みアセットバンドルのハッシュ値取得 ------

                    var cachedFileLastWriteTimeTable = await BuildAssetBundle.GetCachedFileLastWriteTimeTable();

                    AddBuildTimeLog(logBuilder, sw, "GetCachedFileLastWriteTimeTable");

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

                    AssetInfoManifestGenerator.SetAssetBundleFileInfo(assetBundlePath, assetInfoManifest);

                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                    AssetInfoManifestGenerator.SetCriAssetFileInfo(exportPath, assetInfoManifest);

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

                    //------ 更新予定のパッケージファイルを削除 ------

                    var deleteTargets = await BuildAssetBundle.GetUpdateTargetAssetInfo(assetInfoManifest, cachedFileLastWriteTimeTable);

                    if (deleteTargets.Any())
                    {
                        BuildAssetBundle.DeleteUpdateTargetPackage(deleteTargets);
                    }

                    AddBuildTimeLog(logBuilder, sw, "DeleteUpdateTargetPackage");

                    //------ AssetBundleファイルをパッケージ化 ------

                    var cryptKey = manageConfig.CryptKey;
                    var cryptIv = manageConfig.CryptIv;

                    await BuildAssetBundle.BuildPackage(exportPath, assetInfoManifest, cryptKey, cryptIv);

                    AddBuildTimeLog(logBuilder, sw, "BuildPackage");
                }

                versionHash = assetInfoManifest.VersionHash;

                //------ ログ出力------

                // ビルド情報.

                var buildLogText = new StringBuilder();

                buildLogText.Append("Build ExternalResource Complete.").AppendLine();
                buildLogText.AppendLine();
                buildLogText.AppendFormat("VersionHash : {0}", versionHash).AppendLine();
                buildLogText.AppendLine();
                buildLogText.AppendLine(logBuilder.ToString());
                buildLogText.AppendLine();

                UnityConsole.Event(ExternalResources.ConsoleEventName, ExternalResources.ConsoleEventColor, buildLogText.ToString());

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

            return versionHash;
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
