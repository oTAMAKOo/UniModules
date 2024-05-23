
using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Modules.Devkit.Project;

namespace Modules.ExternalAssets
{
    public sealed class ManifestUpdateAssetPostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----

        private static string manifestAssetPath = null;

        private static string externalAssetPath = null;

        private static bool initialized = false;

        //----- property -----

        //----- method -----

        private static void Initialize()
        {
            if (initialized){ return; }

            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null) { return; }

            externalAssetPath = projectResourceFolders.ExternalAssetPath;

            manifestAssetPath = AssetInfoManifestGenerator.GetManifestPath(externalAssetPath);

            initialized = true;
        }

        public override int GetPostprocessOrder()
        {
            return 65;
        }

        /// <summary> アセットインポート完了後のコールバック. </summary>
        /// <param name="importedAssets"> インポートされたアセットのファイルパス。 </param>
        /// <param name="deletedAssets"> 削除されたアセットのファイルパス。 </param>
        /// <param name="movedAssets"> 移動されたアセットのファイルパス。 </param>
        /// <param name="movedFromPath"> 移動されたアセットの移動前のファイルパス。 </param>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {
            var assetInfoManifestAutoUpdater = AssetInfoManifestAutoUpdater.Instance;

            if (!assetInfoManifestAutoUpdater.Enable){ return; }

            Initialize();

            try
            {
                var request = false;

                foreach (var path in importedAssets)
                {
                    request |= IsExternalAssetManageTarget(path);
                }

                foreach (var path in deletedAssets)
                {
                    request |= IsExternalAssetManageTarget(path);
                }

                foreach (var path in movedAssets)
                {
                    request |= IsExternalAssetManageTarget(path);
                }

                foreach (var path in movedFromPath)
                {
                    request |= IsExternalAssetManageTarget(path);
                }

                if (request)
                {
                    AssetInfoManifestAutoUpdater.Prefs.requestGenerate = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static bool IsExternalAssetManageTarget(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)){ return false; }

            if (manifestAssetPath == assetPath) { return false; }

            if (!assetPath.StartsWith(externalAssetPath)) { return false; }

            return true;
        }
    }
}