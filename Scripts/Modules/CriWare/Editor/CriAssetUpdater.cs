
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.IO;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;
using Modules.Devkit.Project;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

using Modules.Sound.Editor;

#endif

#if ENABLE_CRIWARE_SOFDEC

using Modules.Movie.Editor;

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

        public static void ExecuteAll()
        {
            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

            ExecuteSoundAssets();

            #endif

            #if ENABLE_CRIWARE_SOFDEC
            
            ExecuteMovieAssets();

            #endif
        }

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

        public static void ExecuteSoundAssets()
        {
            var projectUnityFolders = ProjectUnityFolders.Instance;
            var projectScriptFolders = ProjectScriptFolders.Instance;
			
            var scriptPath = projectScriptFolders.ConstantsScriptPath;
            var streamingAssetPath = projectUnityFolders.StreamingAssetPath;
            
            var streamingAssetFolderName = Path.GetFileName(streamingAssetPath);

            var criAssetConfig = CriAssetConfig.Instance;

            UpdateSoundAssets(criAssetConfig, scriptPath, streamingAssetFolderName);

            UnityConsole.Event(CriWareConsoleEvent.Name, CriWareConsoleEvent.Color, "UpdateSoundAssets Complete.");
        }

        /// <summary>
        /// サウンドアセットをCriの成果物置き場からUnityの管理下にインポート.
        /// </summary>
        private static void UpdateSoundAssets(CriAssetConfig config, string scriptPath, string streamingAssetFolderName)
        {
            var scriptNamespace = config.ScriptNamespace;
			var folderName = config.SoundFolderName;

			var assetExtensions = new string[] { CriAssetDefinition.AcbExtension, CriAssetDefinition.AwbExtension };

            UpdateAcfAsset(config.AcfAssetSourceFullPath, config.AcfAssetExportPath);

            var streamingAssetPath = PathUtility.Combine(UnityPathUtility.AssetsFolder, streamingAssetFolderName);

			var assetFolderPath = PathUtility.Combine(streamingAssetPath, folderName);

			var destFolderPath = AssetDatabase.GUIDToAssetPath(config.InternalSound.destFolderGuid);

			if (!destFolderPath.StartsWith(streamingAssetPath))
			{
				throw new Exception($"Require start StreamingAssets directory.\nAssetFolderPath:{assetFolderPath}");
			}

            var updateScript = UpdateCriAssets(config.InternalSound, assetExtensions);

			UpdateCriAssets(config.ExternalSound, assetExtensions);

            if (updateScript)
            {
				SoundScriptGenerator.Generate(scriptPath, scriptNamespace, assetFolderPath, folderName);
            }
        }

        #endif

        #if ENABLE_CRIWARE_SOFDEC

        public static void ExecuteMovieAssets()
        {
            var projectUnityFolders = ProjectUnityFolders.Instance;
            var projectScriptFolders = ProjectScriptFolders.Instance;
			
            var scriptPath = projectScriptFolders.ConstantsScriptPath;
            var streamingAssetPath = projectUnityFolders.StreamingAssetPath;
            
            var streamingAssetFolderName = Path.GetFileName(streamingAssetPath);

            var criAssetConfig = CriAssetConfig.Instance;

            UpdateMovieAssets(criAssetConfig, scriptPath, streamingAssetFolderName);

            UnityConsole.Event(CriWareConsoleEvent.Name, CriWareConsoleEvent.Color, "UpdateMovieAssets Complete.");
        }

        /// <summary>
        /// ムービーアセットをCriの成果物置き場からUnityの管理下にインポート.
        /// </summary>
        private static void UpdateMovieAssets(CriAssetConfig config, string scriptPath, string streamingAssetFolderName)
        {
            var scriptNamespace = config.ScriptNamespace;
			var folderName = config.MovieFolderName;

			var assetExtensions = new string[] { CriAssetDefinition.UsmExtension };
            
            var streamingAssetPath = PathUtility.Combine(UnityPathUtility.AssetsFolder, streamingAssetFolderName);

            var assetFolderPath = PathUtility.Combine(streamingAssetPath, folderName);

            var destFolderPath = AssetDatabase.GUIDToAssetPath(config.InternalMovie.destFolderGuid);

            if (!destFolderPath.StartsWith(streamingAssetPath))
            {
                throw new Exception($"Require start StreamingAssets directory.\nAssetFolderPath:{assetFolderPath}");
            }

			var updateScript = UpdateCriAssets(config.InternalMovie, assetExtensions);

			UpdateCriAssets(config.ExternalMovie, assetExtensions);

			if (updateScript)
            {
                MovieScriptGenerator.Generate(scriptPath, scriptNamespace, assetFolderPath, folderName);
            }
        }

        #endif

		private static void UpdateAcfAsset(string acfAssetSourceFullPath, string acfAssetExportPath)
		{
			if (string.IsNullOrEmpty(acfAssetSourceFullPath)){ return; }

			var fileName = Path.GetFileName(acfAssetSourceFullPath);
			var exportPath = PathUtility.Combine(acfAssetExportPath, fileName);

			if (FileCopy(acfAssetSourceFullPath, exportPath))
			{
				Debug.LogFormat("Copy AcfAsset: \n{0}", exportPath);
			}
		}

		private static bool UpdateCriAssets(ImportInfo importInfo, string[] assetExtensions)
		{
			var changed = false;
            
			if (string.IsNullOrEmpty(importInfo.sourceFolderRelativePath)){ return false; }

			var projectFolder = UnityPathUtility.GetProjectFolderPath();

			var sourceDir = PathUtility.RelativePathToFullPath(projectFolder, importInfo.sourceFolderRelativePath);

			var assetDir = AssetDatabase.GUIDToAssetPath(importInfo.destFolderGuid);

			using (new AssetEditingScope())
			{
				changed |= ImportCriAsset(sourceDir, assetDir, assetExtensions);
				changed |= DeleteCriAsset(sourceDir, assetDir);
			}

			return changed;
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
                Debug.LogWarningFormat("Path NotFound. {0}", sourceFolderPath);
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
                log.AppendLine("ImportCriAssets:");

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
					using (new DisableStackTraceScope())
					{
						Debug.Log(log.ToString());
					}
                }
            }

            return true;
        }

        private static bool DeleteCriAsset(string sourceFolderPath, string assetFolderPath)
        {
			var delete = false;

            if (string.IsNullOrEmpty(sourceFolderPath) || string.IsNullOrEmpty(assetFolderPath))
            {
                Debug.LogError("DeleteCriAsset Error.");
                return false;
            }

            sourceFolderPath += PathUtility.PathSeparator;
            assetFolderPath = PathUtility.Combine(UnityPathUtility.GetProjectFolderPath(), assetFolderPath) + PathUtility.PathSeparator;

            if (!Directory.Exists(assetFolderPath)) { return false; }

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
                    return false;
                }

                for (var i = 0; i < deleteTargets.Length; i++)
                {
                    var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(deleteTargets[i].Item1);

                    AssetDatabase.DeleteAsset(assetPath);

					delete = true;
                }

				using (new DisableStackTraceScope())
				{
					Debug.LogFormat("Delete CriAssets:\n{0}", builder.ToString());
				}
            }

            var deleteDirectorys = DirectoryUtility.DeleteEmpty(assetFolderPath);

            if (deleteDirectorys.Any())
            {
                var builder = new StringBuilder();
                deleteDirectorys.ForEach(x => builder.AppendLine(x));

				using (new DisableStackTraceScope())
				{
					Debug.LogFormat("Delete Empty Directory:\n{0}", builder.ToString());
				}
            }

			return delete;
        }

        private static bool FileCopy(string sourcePath, string destPath)
        {
			if (string.IsNullOrEmpty(sourcePath)){ return false; }

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

