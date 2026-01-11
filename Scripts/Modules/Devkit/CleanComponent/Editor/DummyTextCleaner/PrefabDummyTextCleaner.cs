
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;

namespace Modules.Devkit.CleanComponent
{
    public static class PrefabDummyTextCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void CleanAllPrefabContents()
        {
            var assetPaths = UnityEditorUtility.FindAssetPathsByType("t:prefab");

            var cleanTargets = new List<string>();

            using (new AssetEditingScope())
            {
                foreach (var assetPath in assetPaths)
                {
                    try
                    {
                        var changed = ModifyPrefabContents(assetPath);

                        if (changed)
                        {
                            cleanTargets.Add(assetPath);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(new Exception($"Prefab : {assetPath}", e));
                    }
                }
            }

            if(cleanTargets.Any())
            {
                var logBuilder = new StringBuilder();

                cleanTargets.ForEach(x => logBuilder.AppendLine(x));

                using (new DisableStackTraceScope())
                {
                    var title = "DummyText cleaned prefabs:";
                    var logs = logBuilder.ToString();

                    LogUtility.ChunkLog(logs, title, x => UnityConsole.Info(x));
                }
            }
        }

        public static bool ModifyPrefabContents(string assetPath)
        {
            var prefab = PrefabUtility.LoadPrefabContents(assetPath);

            if (prefab == null){ return false; }

            var changed = DummyTextCleaner.ModifyComponents(prefab);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, assetPath);
            }

            PrefabUtility.UnloadPrefabContents(prefab);

            return changed;
        }
    }
}