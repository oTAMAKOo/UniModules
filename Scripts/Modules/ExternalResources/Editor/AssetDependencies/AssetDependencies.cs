
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.ExternalResource.Editor
{
    public class AssetDependencies
    {
        public static bool Validate(string externalResourcesPath)
        {
            var manifestPath = PathUtility.Combine(externalResourcesPath, AssetInfoManifest.ManifestFileName);
            var assetInfoManifest = AssetDatabase.LoadAssetAtPath<AssetInfoManifest>(manifestPath);

            var allAssetInfos = assetInfoManifest.GetAssetInfos().ToArray();

            foreach (var assetInfo in allAssetInfos)
            {
                var assetPath = PathUtility.Combine(externalResourcesPath, assetInfo.ResourcePath);

                var dependencies = AssetDatabase.GetDependencies(assetPath);

                if (dependencies.Any(x => !x.StartsWith(externalResourcesPath)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
