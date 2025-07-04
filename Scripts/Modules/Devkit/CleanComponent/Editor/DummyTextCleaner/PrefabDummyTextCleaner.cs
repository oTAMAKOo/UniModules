
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions;
using Extensions.Devkit;

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
            var prefabs = UnityEditorUtility.FindAssetsByType<GameObject>("t:prefab");

            var cleanTargets = new List<string>();

            using (new AssetEditingScope())
            {
                foreach (var prefab in prefabs)
                {
                    var changed = ModifyPrefabContents(prefab);

                    if (changed)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(prefab);

                        cleanTargets.Add(assetPath);
                    }
                }
            }

            if(cleanTargets.Any())
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine("DummyText cleaned prefabs:");

                cleanTargets.ForEach(x => logBuilder.AppendLine(x));

                using (new DisableStackTraceScope())
                {
                    Debug.LogWarning(logBuilder.ToString());
                }
            }
        }

        public static bool ModifyPrefabContents(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            return ModifyPrefabContents(prefab);
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