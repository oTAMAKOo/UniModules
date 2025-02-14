
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Modules.Devkit.SceneImporter;
using Modules.Devkit.Project;
using Extensions;

namespace Modules.Devkit.Generators
{
	public sealed class ScenesInBuildGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

	    public static void Generate()
	    {
            var projectScriptFolders = ProjectScriptFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            var managedFolderPaths = sceneImporterConfig.GetManagedFolderPaths();

	        var assetsFolderPath = UnityPathUtility.DataPath.Substring(0, UnityPathUtility.DataPath.Length - "Assets".Length);

            var buildTargetScenes = EditorBuildSettings.scenes
                .Where(x => File.Exists(assetsFolderPath + x.path))
                .ToDictionary(x => x.path);

	        var isChanged = EditorBuildSettings.scenes.Length != buildTargetScenes.Count;

            foreach (var folder in managedFolderPaths)
	        {
	            var folderPath = assetsFolderPath + folder;

	            var directoryInfo = new DirectoryInfo(folderPath);
	            var files = directoryInfo.GetFiles("*" + SceneImporterConfig.SceneFileExtension, SearchOption.AllDirectories);

	            foreach (var file in files)
	            {
	                var assetPath = file.FullName.Replace("\\", "/").Replace(assetsFolderPath, string.Empty);

                    if (buildTargetScenes.ContainsKey(assetPath)){ continue; }

                    buildTargetScenes[assetPath] = new EditorBuildSettingsScene(assetPath, true);
                    isChanged = true;
	            }
	        }

	        if (isChanged)
	        {
	            UpdateBuildTargetScenes(sceneImporterConfig, projectScriptFolders.ConstantsScriptPath, buildTargetScenes);
	        }
	        else
	        {
                EditorUtility.DisplayDialog("ScenesInBuild", "Build target scene is up to date.", "close");
            }
	    }

        public static void UpdateBuildTargetScenes(SceneImporterConfig sceneImporterConfig, string scriptPath, Dictionary<string, EditorBuildSettingsScene> buildTargetScenes)
        {
            var initialScenePath = sceneImporterConfig.GetInitialScenePath();
            var managedFolderPaths = sceneImporterConfig.GetManagedFolderPaths();

            var nonInitialScenes = buildTargetScenes.Values
                    .Where(x => x.path != initialScenePath)
                    .OrderBy(x => !managedFolderPaths.Any(y => x.path.Contains(y)))
                    .ThenBy(x => x.path).ToList();

            EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(initialScenePath, true) }.Concat(nonInitialScenes).ToArray();
            ScenesScriptGenerator.Generate(managedFolderPaths, scriptPath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("ScenesInBuild", "Build target scene is update.", "close");
        }
    }
}
