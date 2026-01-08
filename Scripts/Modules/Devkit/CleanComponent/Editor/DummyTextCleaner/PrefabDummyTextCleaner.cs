
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
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

        public static async UniTask CleanAllPrefabContents()
        {
            var assetPaths = UnityEditorUtility.FindAssetPathsByType("t:prefab");

            var cleanTargets = new List<string>();

            using (new AssetEditingScope())
            {
                var chunk = assetPaths.Chunk(150);

                foreach (var items in chunk)
                {
                    foreach (var item in items)
                    {
                        try
                        {
                            var prefab = PrefabUtility.LoadPrefabContents(item);

                            var changed = ModifyPrefabContents(prefab);

                            if (changed)
                            {
                                var assetPath = AssetDatabase.GetAssetPath(prefab);

                                cleanTargets.Add(assetPath);
                            }

                            PrefabUtility.UnloadPrefabContents(prefab);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(new Exception($"Prefab : {item}", e));
                        }
                    }

                    await UniTask.NextFrame();
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

        public static bool ModifyPrefabContents(GameObject prefab)
        {
            if (prefab == null) { return false; }

            var changed = DummyTextCleaner.ModifyComponents(prefab);

            if (changed)
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }

            return changed;
        }
    }
}