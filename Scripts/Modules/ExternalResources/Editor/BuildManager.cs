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
using Modules.Devkit.Project;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare.Editor;

#endif

namespace Modules.ExternalResource.Editor
{
    public static class BuildManager
    {
        //----- params -----

        private static class Prefs
        {
            public static string exportPath
            {
                get { return ProjectPrefs.GetString("ExternalResourceManager-Prefs-exportPath", UnityPathUtility.GetProjectFolderPath()); }
                set { ProjectPrefs.SetString("ExternalResourceManager-Prefs-exportPath", value); }
            }
        }

        private const string ExportFolderName = "ExternalResources";

        private static readonly string[] IgnoreDependentCheckExtensions = { ".cs" };

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

                var cryptoKey = manageConfig.CryptoKey;
                var cryptoIv = manageConfig.CryptoIv;

                var assetBundlePath = BuildAssetBundle.GetAssetBundleOutputPath();

                // 暗号化鍵情報の変更チェック.

                var cryptoChanged = BuildAssetBundlePackage.CheckCryptoFile(assetBundlePath, cryptoKey, cryptoIv);

                using (new DisableStackTraceScope())
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    //------ アセットバンドル名を設定------

                    assetManagement.ApplyAllAssetBundleName();

                    AddBuildTimeLog(logBuilder, sw, "ApplyAllAssetBundleName");

                    //------ キャッシュ済みアセットバンドルの最終更新日時取得 ------

                    var cachedFileLastWriteTimeTable = await BuildAssetBundle.GetCachedFileLastWriteTimeTable();

                    AddBuildTimeLog(logBuilder, sw, "GetCachedFileLastWriteTimeTable");
                    
                    //------ CRIアセットを生成 ------

                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                    CriAssetGenerator.Generate(exportPath, assetInfoManifest);

                    AddBuildTimeLog(logBuilder, sw, "GenerateCriAsset");

                    #endif

                    //------ AssetBundleをビルド ------

                    var assetBundleManifest = BuildAssetBundle.BuildAllAssetBundles();

                    BuildAssetBundle.CreateTemporarilyAssetBundleManifestFile();

                    AddBuildTimeLog(logBuilder, sw, "BuildAllAssetBundles");

                    //------ 不要になった古いAssetBundle削除 ------

                    BuildAssetBundle.CleanUnUseAssetBundleFiles();

                    AddBuildTimeLog(logBuilder, sw, "CleanUnUseAssetBundleFiles");

                    //------ AssetBundleファイルをパッケージ化 ------

                    // 暗号化鍵情報の書き込み.

                    BuildAssetBundlePackage.CreateCryptoFile(assetBundlePath, cryptoKey, cryptoIv);

                    // 更新対象のアセット情報取得.

                    var assetInfos = BuildAssetBundle.GetAllTargetAssetInfo(assetInfoManifest);

                    var updatedAssetInfos = new AssetInfo[0];

                    if (!cryptoChanged)
                    {
                        updatedAssetInfos = await BuildAssetBundle.GetUpdateTargetAssetInfo(assetInfoManifest, cachedFileLastWriteTimeTable);
                    }

                    // パッケージファイル作成.

                    await BuildAssetBundlePackage.BuildAllAssetBundlePackage(exportPath, assetBundlePath, assetInfos, updatedAssetInfos, cryptoKey, cryptoIv);

                    AddBuildTimeLog(logBuilder, sw, "BuildPackage");

                    //------ ビルド成果物の情報をAssetInfoManifestに書き込み ------

                    await AssetInfoManifestGenerator.SetAssetBundleFileInfo(assetBundlePath, assetInfoManifest);

                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                    await AssetInfoManifestGenerator.SetCriAssetFileInfo(exportPath, assetInfoManifest);

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

                    //------ AssetInfoManifestファイルをパッケージ化 ------

                    await BuildAssetBundlePackage.BuildAssetInfoManifestPackage(exportPath, assetBundlePath, cryptoKey, cryptoIv);

                    AddBuildTimeLog(logBuilder, sw, "BuildPackage AssetInfoManifest");
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

            var platformAssetFolderName = PlatformUtility.GetPlatformTypeName();

            return PathUtility.Combine(path, platformAssetFolderName) + PathUtility.PathSeparator;
        }

        public static bool AssetDependenciesValidate(AssetInfoManifest assetInfoManifest)
        {
            var projectFolders = ProjectFolders.Instance;

            var config = ManageConfig.Instance;

            var externalResourcesPath = projectFolders.ExternalResourcesPath;

            var allAssetInfos = assetInfoManifest.GetAssetInfos().ToArray();

            var ignoreValidatePaths = config.IgnoreValidateTarget
                  .Where(x => x != null)
                  .Select(x => AssetDatabase.GetAssetPath(x))
                  .ToArray();

            Func<string, bool> checkInvalid = path =>
            {
                // 除外対象拡張子はチェック対象外.

                var extension = Path.GetExtension(path);

                if (IgnoreDependentCheckExtensions.Any(y => y == extension)) { return false; }

                // 除外対象.

                if (ignoreValidatePaths.Any(x => path.StartsWith(x))) { return false; }

                // 外部アセット対象ではない.

                if (!path.StartsWith(externalResourcesPath)) { return true; }

                return false;
            };

            using (new DisableStackTraceScope())
            {
                foreach (var assetInfo in allAssetInfos)
                {
                    var assetPath = PathUtility.Combine(externalResourcesPath, assetInfo.ResourcePath);

                    var dependencies = AssetDatabase.GetDependencies(assetPath);

                    var invalidDependencies = dependencies.Where(x => checkInvalid(x)).ToArray();

                    if (invalidDependencies.Any())
                    {
                        var builder = new StringBuilder();

                        builder.AppendFormat("Asset: {0}", assetPath).AppendLine();
                        builder.AppendLine("Invalid Dependencies:");

                        foreach (var item in invalidDependencies)
                        {
                            builder.AppendLine(item);
                        }

                        Debug.LogWarningFormat(builder.ToString());

                        return false;
                    }
                }
            }

            return true;
        }
    }
}
