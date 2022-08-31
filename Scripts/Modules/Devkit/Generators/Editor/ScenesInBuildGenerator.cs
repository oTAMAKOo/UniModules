
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Modules.Devkit.SceneImporter;
using Modules.Devkit.Project;

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

            var managedFolders = sceneImporterConfig.ManagedFolders;

	        var assetsFolderPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);

            var buildTargetScenes = EditorBuildSettings.scenes
                .Where(x => File.Exists(assetsFolderPath + x.path))
                .ToDictionary(x => x.path);

	        var isChanged = EditorBuildSettings.scenes.Length != buildTargetScenes.Count;

            foreach (var folder in managedFolders)
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
	            UpdateBuildTargetScenes(sceneImporterConfig, projectScriptFolders.ScriptPath, buildTargetScenes);
	        }
	        else
	        {
                EditorUtility.DisplayDialog("ScenesInBuildSetter", "ビルドターゲットのシーンは最新の状態です", "確認");
            }
	    }

        public static void UpdateBuildTargetScenes(SceneImporterConfig sceneImporterConfig, string scriptPath, Dictionary<string, EditorBuildSettingsScene> buildTargetScenes)
        {
            var initialScene = sceneImporterConfig.InitialScene;
            var managedFolders = sceneImporterConfig.ManagedFolders;

            var nonInitialScenes = buildTargetScenes.Values
                    .Where(x => x.path != initialScene)
                    .OrderBy(x => !sceneImporterConfig.ManagedFolders.Any(y => x.path.Contains(y)))
                    .ThenBy(x => x.path).ToList();

            EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(initialScene, true) }.Concat(nonInitialScenes).ToArray();
            ScenesScriptGenerator.Generate(managedFolders, scriptPath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("SceneAssetPostprocessor", "EditorBuildSettingsの更新、SceneNamesの再出力を行いました", "確認");
        }
    }
}
