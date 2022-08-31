﻿
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.AssetBundles.Editor;
using Modules.Devkit.Console;
using Modules.Devkit.Project;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare.Editor;

#endif

namespace Modules.ExternalResource
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

        public static IBuildAssetBundlePipeline BundlePipeline { get; } = new BuildAssetBundlePipeline();

        //----- property -----

        //----- method -----

        public static bool BuildConfirm()
        {
            return EditorUtility.DisplayDialog("Confirmation", "外部アセットを生成します.", "実行", "中止");
        }

        public static async UniTask<string> Build(string exportPath, AssetInfoManifest assetInfoManifest, bool openExportFolder = true)
        {
            if (string.IsNullOrEmpty(exportPath)) { return null; }

            if (Directory.Exists(exportPath))
            {
                Directory.Delete(exportPath, true);
            }

            var versionHash = string.Empty;

            var assetManagement = AssetManagement.Instance;

            assetManagement.Initialize();

            var buildAssetBundle = new BuildAssetBundle(BundlePipeline);

            EditorApplication.LockReloadAssemblies();

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var logBuilder = new StringBuilder();

                var manageConfig = ManageConfig.Instance;

                var cryptoKey = manageConfig.CryptoKey;
                var cryptoIv = manageConfig.CryptoIv;

                var assetBundlePath = buildAssetBundle.GetAssetBundleOutputPath();

                // 暗号化鍵情報の変更チェック.

                var cryptoChanged = BuildAssetBundlePackage.CheckCryptoFile(assetBundlePath, cryptoKey, cryptoIv);

                using (new DisableStackTraceScope())
                {
                    var processTime = System.Diagnostics.Stopwatch.StartNew();

                    //------ アセットバンドル名を設定------

                    using (new BuildLogScope(logBuilder, processTime, "ApplyAllAssetBundleName"))
                    {
                        assetManagement.ApplyAllAssetBundleName();
                    }

                    //------ キャッシュ済みアセットバンドルの最終更新日時取得 ------

                    Dictionary<string, DateTime> cachedFileLastWriteTimeTable = null;

                    using (new BuildLogScope(logBuilder, processTime, "GetCachedFileLastWriteTimeTable"))
                    {
                        cachedFileLastWriteTimeTable = buildAssetBundle.GetCachedFileLastWriteTimeTable();
                    }

                    //------ CRIアセットを生成 ------

                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                    using (new BuildLogScope(logBuilder, processTime, "GenerateCriAsset"))
                    {
                        CriAssetGenerator.Generate(exportPath, assetInfoManifest);
                    }

                    #endif

                    //------ AssetBundleをビルド ------

                    BuildResult buildResult = null;

                    using (new BuildLogScope(logBuilder, processTime, "BuildAllAssetBundles"))
                    {
                        buildResult = buildAssetBundle.BuildAllAssetBundles();
                    }

                    if (!buildResult.IsSuccess)
                    {
                        Debug.LogErrorFormat("Build ExternalResource failed.\n{0}", buildResult.ExitCode);

                        return null;
                    }

                    //------ 未登録のアセットバンドル情報追加 ------

                    using (new BuildLogScope(logBuilder, processTime, "AddUnregisteredAssetInfos"))
                    {
                        buildAssetBundle.AddUnregisteredAssetInfos(assetInfoManifest, buildResult);
                    }

                    //------ 不要になった古いAssetBundle削除 ------

                    using (new BuildLogScope(logBuilder, processTime, "CleanUnUseAssetBundleFiles"))
                    {
                        buildAssetBundle.CleanUnUseAssetBundleFiles(buildResult);
                    }

                    //------ AssetBundleファイルをパッケージ化 ------

                    // 暗号化鍵情報の書き込み.

                    using (new BuildLogScope(logBuilder, processTime, "CreateCryptoFile"))
                    {
                        BuildAssetBundlePackage.CreateCryptoFile(assetBundlePath, cryptoKey, cryptoIv);
                    }

                    // 更新対象のアセット情報取得.

                    var assetInfos = new AssetInfo[0];
                    var updatedAssetInfos = new AssetInfo[0];

                    using (new BuildLogScope(logBuilder, processTime, "GetUpdateTargetAssetInfo"))
                    {
                        assetInfos = buildAssetBundle.GetAllTargetAssetInfo(assetInfoManifest);

                        // 暗号化キーが変わっていたら全て更新対象.
                        if (cryptoChanged)
                        {
                            updatedAssetInfos = assetInfos;
                        }
                        // 差分がある対象だけ抽出.
                        else
                        {
                            updatedAssetInfos = buildAssetBundle.GetUpdateTargetAssetInfo(assetInfoManifest, cachedFileLastWriteTimeTable);
                        }
                    }

                    // パッケージファイル作成.

                    using (new BuildLogScope(logBuilder, processTime, "BuildPackage"))
                    {
                        await BuildAssetBundlePackage.BuildAllAssetBundlePackage(exportPath, assetBundlePath, assetInfos, updatedAssetInfos, cryptoKey, cryptoIv);
                    }

                    //------ ビルド成果物の情報をAssetInfoManifestに書き込み ------

                    using (new BuildLogScope(logBuilder, processTime, "AssetInfoManifest : SetAssetBundleFileInfo"))
                    {
                        await AssetInfoManifestGenerator.SetAssetBundleFileInfo(assetBundlePath, assetInfoManifest, buildResult);

                        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

                        await AssetInfoManifestGenerator.SetCriAssetFileInfo(exportPath, assetInfoManifest);

                        #endif
                    }

                    //------ アセットバンドルの参照情報をAssetInfoManifestに書き込み ------

                    using (new BuildLogScope(logBuilder, processTime, "AssetInfoManifest : SetAssetBundleDependencies"))
                    {
                        buildAssetBundle.SetDependencies(assetInfoManifest, buildResult);
                    }

                    //------ バージョンハッシュ情報をAssetInfoManifestに書き込み ------

                    using (new BuildLogScope(logBuilder, processTime, "AssetInfoManifest : SetAssetInfoHash"))
                    {
                        buildAssetBundle.SetAssetInfoHash(assetInfoManifest);
                    }

                    //------ 再度AssetInfoManifestだけビルドを実行 ------

                    using (new BuildLogScope(logBuilder, processTime, "Rebuild AssetInfoManifest"))
                    {
                        buildAssetBundle.BuildAssetInfoManifest();
                    }

                    //------ AssetInfoManifestファイルをパッケージ化 ------

                    using (new BuildLogScope(logBuilder, processTime, "BuildPackage AssetInfoManifest"))
                    {
                        await BuildAssetBundlePackage.BuildAssetInfoManifestPackage(exportPath, assetBundlePath, cryptoKey, cryptoIv);
                    }
                }

                versionHash = assetInfoManifest.VersionHash;

                //------ バージョンをファイル出力------

                GenerateVersionFile(exportPath, versionHash);

                //------ ログ出力------

                stopwatch.Stop();

                // ビルド情報.

                var buildLogText = new StringBuilder();

                var totalSeconds = stopwatch.Elapsed.TotalSeconds; 

                buildLogText.AppendFormat("Build ExternalResource Complete. ({0:F2}sec)", totalSeconds).AppendLine();
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
            var projectResourceFolders = ProjectResourceFolders.Instance;

            var config = ManageConfig.Instance;

            var externalResourcesPath = projectResourceFolders.ExternalResourcesPath;

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
