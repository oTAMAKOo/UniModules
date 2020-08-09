
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

using Modules.Devkit;
using Modules.Devkit.Project;

#if ENABLE_CRIWARE_ADX

using Modules.SoundManagement.Editor;

#endif

#if ENABLE_CRIWARE_SOFDEC

using Modules.MovieManagement.Editor;

#endif

using DirectoryUtility = Extensions.DirectoryUtility;

namespace Modules.CriWare.Editor
{
    public static class CriAssetUpdater
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Execute()
        {
            var editorConfig = ProjectFolders.Instance;

            var scriptPath = editorConfig.ScriptPath;
            var streamingAssetPath = editorConfig.StreamingAssetPath;
            var externalResourcesPath = editorConfig.ExternalResourcesPath;

            var streamingAssetFolderName = Path.GetFileName(streamingAssetPath);
            var externalResourcesFolderName = Path.GetFileName(externalResourcesPath);

            var criAssetConfig = CriAssetConfig.Instance;

            #if ENABLE_CRIWARE_ADX

            UpdateSoundAssets(criAssetConfig, scriptPath, streamingAssetFolderName, externalResourcesFolderName);

            #endif

            #if ENABLE_CRIWARE_SOFDEC
            
            UpdateMovieAssets(criAssetConfig, scriptPath, streamingAssetFolderName, externalResourcesFolderName);

            #endif

            UnityConsole.Event(CriWareConsoleEvent.Name, CriWareConsoleEvent.Color, "UpdateCriAssets Complete.");
        }

        #if ENABLE_CRIWARE_ADX

        /// <summary>
        /// サウンドアセットをCriの成果物置き場からUnityの管理下にインポート.
        /// </summary>
        private static void UpdateSoundAssets(CriAssetConfig config, string scriptPath, string streamingAssetFolderName, string externalResourcesFolderName)
        {
            var importPath = config.SoundImportInfo.ImportPath;
            var folderName = config.SoundImportInfo.FolderName;

            var assetExtensions = new string[] { CriAssetDefinition.AcbExtension, CriAssetDefinition.AwbExtension };

            var assetDirInternal = PathUtility.Combine(new string[] { UnityPathUtility.AssetsFolder, streamingAssetFolderName, folderName });

            UpdateAcfAsset(config.AcfAssetSourceFullPath, config.AcfAssetExportPath);

            var updateScript = UpdateCriAssets(
                    importPath, folderName,
                    streamingAssetFolderName, externalResourcesFolderName,
                    assetExtensions
                    );

            if (updateScript)
            {
                SoundScriptGenerator.Generate(scriptPath, assetDirInternal, folderName);
            }
        }

        #endif

        #if ENABLE_CRIWARE_SOFDEC

        /// <summary>
        /// ムービーアセットをCriの成果物置き場からUnityの管理下にインポート.
        /// </summary>
        private static void UpdateMovieAssets(CriAssetConfig config, string scriptPath, string internalResourcesFolderName, string externalResourcesFolderName)
        {
            var importPath = config.MovieImportInfo.ImportPath;
            var folderName = config.MovieImportInfo.FolderName;

            var assetExtensions = new string[] { CriAssetDefinition.UsmExtension };

            var assetDirInternal = PathUtility.Combine(new string[] { UnityPathUtility.AssetsFolder, internalResourcesFolderName, folderName });

            var updateScript = UpdateCriAssets(
                    importPath, folderName,
                    internalResourcesFolderName, externalResourcesFolderName,
                    assetExtensions
                    );

            if (updateScript)
            {
                MovieScriptGenerator.Generate(scriptPath, assetDirInternal);
            }
        }

        #endif

        private static void UpdateAcfAsset(string acfAssetSourceFullPath, string acfAssetExportPath)
        {
            if (!string.IsNullOrEmpty(acfAssetSourceFullPath))
            {
                var fileName = Path.GetFileName(acfAssetSourceFullPath);
                var exportPath = PathUtility.Combine(acfAssetExportPath, fileName);

                if (FileCopy(acfAssetSourceFullPath, exportPath))
                {
                    Debug.LogFormat("Copy AcfAsset: \n{0}", exportPath);
                }
            }
        }

        private static bool UpdateCriAssets(
            string criExportDir, string rootFolderName,
            string streamingAssetFolderName, string externalResourcesFolderName,
            string[] assetExtensions)
        {
            var criExportDirInternal = PathUtility.Combine(criExportDir, streamingAssetFolderName);
            var criExportDirExternal = PathUtility.Combine(criExportDir, externalResourcesFolderName);

            // アセット置き場のUnity上でのパス生成.
            var assetDirInternal = PathUtility.Combine(new string[] { UnityPathUtility.AssetsFolder, streamingAssetFolderName, rootFolderName });
            var assetDirExternal = PathUtility.Combine(new string[] { UnityPathUtility.AssetsFolder, externalResourcesFolderName, rootFolderName });

            var updateScript = false;

            using (new AssetEditingScope())
            {
                // InternalResources.
                updateScript = ImportCriAsset(criExportDirInternal, assetDirInternal, assetExtensions);
                DeleteCriAsset(criExportDirInternal, assetDirInternal);

                // ExternalResources.
                ImportCriAsset(criExportDirExternal, assetDirExternal, assetExtensions);
                DeleteCriAsset(criExportDirExternal, assetDirExternal);
            }

            return updateScript;
        }

        private static bool ImportCriAsset(string sourceFolderPath, string assetFolderPath, string[] assetExtensions)
        {
            if (string.IsNullOrEmpty(sourceFolderPath) || string.IsNullOrEmpty(assetFolderPath))
            {
                Debug.LogError("ImportCriAsset Error.");
                return false;
            }

            sourceFolderPath += PathUtility.PathSeparator;
            assetFolderPath = PathUtility.Combine(UnityPathUtility.GetProjectFolderPath(), assetFolderPath) + PathUtility.PathSeparator;

            if (!Directory.Exists(sourceFolderPath))
            {
                Debug.LogWarningFormat("Path Notfound. {0}", sourceFolderPath);
                return false;
            }

            var files = Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories);

            var copyTargets = files
                .Where(x => assetExtensions.Contains(Path.GetExtension(x)))
                .Select(x => Tuple.Create(x, PathUtility.Combine(assetFolderPath, x.Replace(sourceFolderPath, string.Empty))))
                .ToArray();

            if (copyTargets.Any())
            {
                var copyCount = 0;

                var log = new StringBuilder();
                log.AppendLine("Copy MovieAssets:");

                for (var i = 0; i < copyTargets.Length; i++)
                {
                    if (FileCopy(copyTargets[i].Item1, copyTargets[i].Item2))
                    {
                        var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(copyTargets[i].Item2);

                        AssetDatabase.ImportAsset(assetPath);

                        log.AppendLine(assetPath);
                        copyCount++;
                    }
                }

                if (0 < copyCount)
                {
                    Debug.Log(log.ToString());
                }
            }

            return true;
        }

        private static void DeleteCriAsset(string sourceFolderPath, string assetFolderPath)
        {
            if (string.IsNullOrEmpty(sourceFolderPath) || string.IsNullOrEmpty(assetFolderPath))
            {
                Debug.LogError("DeleteCriAsset Error.");
                return;
            }

            sourceFolderPath += PathUtility.PathSeparator;
            assetFolderPath = PathUtility.Combine(UnityPathUtility.GetProjectFolderPath(), assetFolderPath) + PathUtility.PathSeparator;

            if (!Directory.Exists(assetFolderPath)) { return; }

            var files = Directory.GetFiles(assetFolderPath, "*", SearchOption.AllDirectories);

            var deleteTargets = files
                .Where(x => Path.GetExtension(x) != ".meta")
                .Select(x => Tuple.Create(x, x.Replace(assetFolderPath, sourceFolderPath)))
                .Where(x => !File.Exists(x.Item2))
                .ToArray();

            if (deleteTargets.Any())
            {
                var builder = new StringBuilder();

                deleteTargets.ForEach(x => builder.AppendLine(x.Item1.ToString()));

                if (!EditorUtility.DisplayDialog("Delete Confirmation", builder.ToString(), "実行", "中止"))
                {
                    return;
                }

                for (var i = 0; i < deleteTargets.Length; i++)
                {
                    var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(deleteTargets[i].Item1);

                    AssetDatabase.DeleteAsset(assetPath);
                }

                Debug.LogFormat("Delete CriAssets:\n{0}", builder.ToString());
            }

            var deleteDirectorys = DirectoryUtility.DeleteEmpty(assetFolderPath);

            if (deleteDirectorys.Any())
            {
                var builder = new StringBuilder();
                deleteDirectorys.ForEach(x => builder.AppendLine(x));

                Debug.LogFormat("Delete Empty Directory:\n{0}", builder.ToString());
            }
        }

        private static bool FileCopy(string sourcePath, string destPath)
        {
            // 更新されてない物はコピーしない.
            if (File.Exists(destPath) && File.GetLastWriteTime(sourcePath) == File.GetLastWriteTime(destPath))
            {
                return false;
            }

            var directory = Path.GetDirectoryName(destPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourcePath, destPath, true);

            return true;
        }
    }
}

#endif
