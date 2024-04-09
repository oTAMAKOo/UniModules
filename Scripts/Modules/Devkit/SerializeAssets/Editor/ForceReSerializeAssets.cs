
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.SerializeAssets
{
    public static class ForceReSerializeAssets
    {
        public static void ExecuteSelectionAssets(ForceReserializeAssetsOptions options = ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata)
        {
            var assetPaths = Selection.objects
                .Where(x => AssetDatabase.IsMainAsset(x))
                .Select(x => AssetDatabase.GetAssetPath(x))
                .ToArray();
                
            Execute(assetPaths, options);
        }

        public static void ExecuteAllPrefabs(ForceReserializeAssetsOptions options = ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata)
        {
            var prefabs = AssetDatabase.FindAssets("t:prefab")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .ToArray();
                
            Execute(prefabs, options);
        }

        public static void Execute(string[] assetPaths, ForceReserializeAssetsOptions options = ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata)
        {
            if (assetPaths == null || assetPaths.IsEmpty()){ return; }
            
            var targets = assetPaths
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToArray();

            if (targets.Any())
            {
                using (new AssetEditingScope())
                {
                    AssetDatabase.ForceReserializeAssets(targets, options);
                }

                AssetDatabase.SaveAssets();
                
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                using (new DisableStackTraceScope())
                {
                    Debug.LogFormat("Finish {0}.", options);
                }
            }
            else
            {
                using (new DisableStackTraceScope())
                {
                    Debug.LogWarning("Require select target assets.");
                }
            }
        }
    }
}
