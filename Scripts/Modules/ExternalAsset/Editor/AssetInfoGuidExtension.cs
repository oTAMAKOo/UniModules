
using UnityEditor;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.Project;

namespace Modules.ExternalAssets
{
    public static class AssetInfoGuidExtension
    {
        //----- params -----

        //----- field -----

        private static Dictionary<string, string> cache = null;

        //----- property -----

        //----- method -----

        public static string GetGuid(this AssetInfo assetInfo)
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null) { return null; }

            if (assetInfo == null) { return null; }

            if (cache == null)
            {
                cache = new Dictionary<string, string>();
            }

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;

            var assetPath = PathUtility.Combine(externalAssetPath, assetInfo.ResourcePath);

            if (cache.ContainsKey(assetPath))
            {
                return cache.GetValueOrDefault(assetPath);
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            cache.Add(assetPath, guid);

            return guid;
        }
    }
}