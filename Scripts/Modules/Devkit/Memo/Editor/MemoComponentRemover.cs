
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Linq;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;

namespace Modules.Devkit.Memo
{
    public static class MemoComponentRemover
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        private static bool RemoveComponents(GameObject rootObject)
        {
            if (rootObject == null){ return false; }

            var components = rootObject.DescendantsAndSelf()
                .OfComponent<Memo>()
                .ToArray();

            foreach (var component in components)
            {
                UnityUtility.DeleteComponent(component, true);
            }

            return components.Any();
        }

        private static bool RemoveSceneComponents(string sceneAssetPath)
        {
            var changed = false;

            var scene = EditorSceneManager.OpenScene(sceneAssetPath);

            var rootObjests = scene.GetRootGameObjects();

            foreach (var rootObjest in rootObjests)
            {
                changed |= RemoveComponents(rootObjest);
            }

            if (changed)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(sceneAssetPath);

                UnityEditorUtility.SaveAsset(asset);
            }

            return changed;
        }

        public static void RemoveAllSceneComponents()
        {
            var scenes = EditorBuildSettings.scenes;

            var targets = new List<string>();

            using (new AssetEditingScope())
            {
                for (var i = 0; i < scenes.Length; i++)
                {
                    var scene = scenes[i];

                    if(!scene.enabled){ continue; }

                    try
                    {
                        var changed = RemoveSceneComponents(scene.path);

                        if (changed)
                        {
                            targets.Add(scene.path);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException((new Exception($"Scene : {scene.path}", e)));
                    }
                }
            }

            if (targets.Any())
            {
                var logBuilder = new StringBuilder();

                targets.ForEach(x => logBuilder.AppendLine(x));

                var title = "Memo component remove at scenes";
                var logs = logBuilder.ToString();

                LogUtility.ChunkLog(logs, title, x => UnityConsole.Info(x));
            }
        }

        public static void RemoveAllPrefabComponents()
        {
            var assetPaths = UnityEditorUtility.FindAssetPathsByType("t:prefab");

            var targets = new List<string>();

            using (new AssetEditingScope())
            {
                foreach (var assetPath in assetPaths)
                {
                    try
                    {
                        var prefab = PrefabUtility.LoadPrefabContents(assetPath);

                        var changed = RemoveComponents(prefab);

                        if (changed)
                        {
                            PrefabUtility.SaveAsPrefabAsset(prefab, assetPath);

                            targets.Add(assetPath);
                        }

                        PrefabUtility.UnloadPrefabContents(prefab);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(new Exception($"Prefab : {assetPath}", e));
                    }
                }
            }
                
            if(targets.Any())
            {
                var logBuilder = new StringBuilder();

                targets.ForEach(x => logBuilder.AppendLine(x));

                var title = "Memo component remove at prefabs";
                var logs = logBuilder.ToString();

                LogUtility.ChunkLog(logs, title, x => UnityConsole.Info(x));
            }
        }
    }
}