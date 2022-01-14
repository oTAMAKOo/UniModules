﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.AssetBundles.Editor;
using Modules.Devkit.Console;
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

        public const string RootHashFileName = "RootHash.txt";

        private static readonly string[] IgnoreDependentCheckExtensions = { ".cs" };

        private sealed class BuildLogScope : Scope
        {
            private string processName = null;
            private StringBuilder logBuilder = null;
            private System.Diagnostics.Stopwatch stopwatch = null;

            public BuildLogScope(StringBuilder logBuilder, System.Diagnostics.Stopwatch stopwatch, string processName)
            {
                this.logBuilder = logBuilder;
                this.stopwatch = stopwatch;
                this.processName = processName;

                UnityConsole.Event(ExternalResources.ConsoleEventName, ExternalResources.ConsoleEventColor, processName);
            }

            protected override void CloseScope()
            {
                stopwatch.Stop();
                
                logBuilder.AppendFormat("{0} : ({1:F1}sec)", processName, stopwatch.Elapsed.TotalSeconds).AppendLine();

                stopwatch.Restart();
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

                var cryptoKey = manageConfig.CryptoKey;
                var cryptoIv = manageConfig.CryptoIv;

                var assetBundlePath = BuildAssetBundle.GetAssetBundleOutputPath();

                // 暗号化鍵情報の変更チェック.

                var cryptoChanged = BuildAssetBundlePackage.CheckCryptoFile(assetBundlePath, cryptoKey, cryptoIv);

                using (new DisableStackTraceScope())
                {
                    using (new AssetEditingScope())
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();

                        //------ アセットバンドル名を設定------

                        using (new BuildLogScope(logBuilder, sw, "ApplyAllAssetBundleName"))
                        {
                            assetManagement.ApplyAllAssetBundleName();
                        }

                        //------ キャッシュ済みアセットバンドルの最終更新日時取得 ------

                        Dictionary<string, DateTime> cachedFileLastWriteTimeTable = null;

                        using (new BuildLogScope(logBuilder, sw, "GetCachedFileLastWriteTimeTable"))
                        {
                            cachedFileLastWriteTimeTable = await BuildAssetBundle.GetCachedFileLastWriteTimeTable();
                        }

                        //------ CRIアセットを生成 ------

                        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                        using (new BuildLogScope(logBuilder, sw, "GenerateCriAsset"))
                        {
                            CriAssetGenerator.Generate(exportPath, assetInfoManifest);
                        }

                        #endif

                        //------ AssetBundleをビルド ------

                        AssetBundleManifest assetBundleManifest = null;

                        using (new BuildLogScope(logBuilder, sw, "BuildAllAssetBundles"))
                        {
                            assetBundleManifest = BuildAssetBundle.BuildAllAssetBundles();

                            BuildAssetBundle.CreateTemporarilyAssetBundleManifestFile();
                        }

                        //------ アセットバンドルの参照情報をAssetInfoManifestに書き込み ------

                        using (new BuildLogScope(logBuilder, sw, "AssetInfoManifest : SetAssetBundleDependencies"))
                        {
                            BuildAssetBundle.SetDependencies(assetInfoManifest, assetBundleManifest);
                        }

                        //------ 不要になった古いAssetBundle削除 ------

                        using (new BuildLogScope(logBuilder, sw, "CleanUnUseAssetBundleFiles"))
                        {
                            BuildAssetBundle.CleanUnUseAssetBundleFiles();
                        }

                        //------ AssetBundleファイルをパッケージ化 ------

                        // 暗号化鍵情報の書き込み.

                        using (new BuildLogScope(logBuilder, sw, "CreateCryptoFile"))
                        {
                            BuildAssetBundlePackage.CreateCryptoFile(assetBundlePath, cryptoKey, cryptoIv);
                        }

                        // 更新対象のアセット情報取得.

                        var assetInfos = new AssetInfo[0];
                        var updatedAssetInfos = new AssetInfo[0];

                        using (new BuildLogScope(logBuilder, sw, "GetUpdateTargetAssetInfo"))
                        {
                            assetInfos = BuildAssetBundle.GetAllTargetAssetInfo(assetInfoManifest);

                            // 暗号化キーが変わっていたら全て更新対象.
                            if (cryptoChanged)
                            {
                                updatedAssetInfos = assetInfos;
                            }
                            // 差分がある対象だけ抽出.
                            else
                            {
                                updatedAssetInfos = await BuildAssetBundle.GetUpdateTargetAssetInfo(assetInfoManifest, cachedFileLastWriteTimeTable);
                            }
                        }

                        // パッケージファイル作成.

                        using (new BuildLogScope(logBuilder, sw, "BuildPackage"))
                        {
                            await BuildAssetBundlePackage.BuildAllAssetBundlePackage(exportPath, assetBundlePath, assetInfos, updatedAssetInfos, cryptoKey, cryptoIv);
                        }

                        //------ ビルド成果物の情報をAssetInfoManifestに書き込み ------

                        using (new BuildLogScope(logBuilder, sw, "AssetInfoManifest : SetAssetBundleFileInfo"))
                        {
                            await AssetInfoManifestGenerator.SetAssetBundleFileInfo(assetBundlePath, assetInfoManifest);

                            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                            await AssetInfoManifestGenerator.SetCriAssetFileInfo(exportPath, assetInfoManifest);

                            #endif
                        }

                        //------ バージョンハッシュ情報をAssetInfoManifestに書き込み ------

                        using (new BuildLogScope(logBuilder, sw, "AssetInfoManifest : SetAssetInfoHash"))
                        {
                            BuildAssetBundle.SetAssetInfoHash(assetInfoManifest);
                        }

                        //------ 再度AssetInfoManifestだけビルドを実行 ------

                        using (new BuildLogScope(logBuilder, sw, "Rebuild AssetInfoManifest"))
                        {
                            BuildAssetBundle.BuildAssetInfoManifest();

                            BuildAssetBundle.RestoreAssetBundleManifestFile();
                        }

                        //------ AssetInfoManifestファイルをパッケージ化 ------

                        using (new BuildLogScope(logBuilder, sw, "BuildPackage AssetInfoManifest"))
                        {
                            await BuildAssetBundlePackage.BuildAssetInfoManifestPackage(exportPath, assetBundlePath, cryptoKey, cryptoIv);
                        }
                    }
                }

                versionHash = assetInfoManifest.VersionHash;

                //------ バージョンをファイル出力------

                GenerateVersionFile(exportPath, versionHash);

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

        public static string GetExportPath()
        {
            var config = ManageConfig.Instance;
            
            var exportDirectory = config.ExportDirectory;

            if (string.IsNullOrEmpty(exportDirectory)) { return null; }
            
            var platformFolderName = PlatformUtility.GetPlatformTypeName();

            var paths = new string[]{ exportDirectory, ExportFolderName, platformFolderName };

            return PathUtility.Combine(paths) + PathUtility.PathSeparator;
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

        private static void GenerateVersionFile(string filePath, string version)
        {
            var path = PathUtility.Combine(filePath, RootHashFileName);

            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) 
            {
                using (var sw = new StreamWriter(fs, Encoding.UTF8)) 
                {
                    sw.Write(version);
                }
            }
        }
    }
}
