
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.SerializeAssets
{
    public static class ForceReSerializeAssets
    {
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
