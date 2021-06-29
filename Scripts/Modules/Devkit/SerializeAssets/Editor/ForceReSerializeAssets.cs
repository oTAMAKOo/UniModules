
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;

namespace Modules.Devkit.SerializeAssets
{
    public static class ForceReSerializeAssets
    {
        public static void Execute(Object[] targetAssets)
        {
            if (targetAssets == null || targetAssets.IsEmpty()){ return; }
            
            var targetAssetPaths = targetAssets
                .Where(x => AssetDatabase.IsMainAsset(x))
                .Select(x => AssetDatabase.GetAssetPath(x))
                .Distinct()
                .ToArray();

            if (targetAssetPaths.Any())
            {
                AssetDatabase.ForceReserializeAssets(targetAssetPaths);
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
