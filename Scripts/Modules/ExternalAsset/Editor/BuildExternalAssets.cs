
using UnityEngine;
using UnityEditor;
using System;
using Cysharp.Threading.Tasks;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare.Editor;

#endif

namespace Modules.ExternalAssets
{
    public static class BuildExternalAssets
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static async UniTask Execute()
        {
            var result = EditorUtility.DisplayDialog("Confirmation", "外部アセットを生成します.", "実行", "中止");

            if (!result){ return; }

            var build = true;

            try
            {
                EditorApplication.LockReloadAssemblies();

                #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

                // CRIの最新アセットに更新.
                CriAssetUpdater.ExecuteAll();

                #endif

                // アセット情報ファイルを生成.
                var assetInfoManifest = await AssetInfoManifestGenerator.Generate();

                // 依存関係の検証.
                var validate = BuildManager.AssetDependenciesValidate(assetInfoManifest);

                if (!validate)
                {
                    var message = "invalid dependencies assets";

                    if (!EditorUtility.DisplayDialog("Warning", message, "force build", "show detail"))
                    {
                        build = false;

                        // ExternalAssetフォルダ以外の参照が含まれる場合は依存関係を表示.
                        InvalidDependantWindow.Open();
                    }
                }

                // ビルド.
                if (build)
                {
                    var exportPath = BuildManager.GetExportPath();

                    if (!string.IsNullOrEmpty(exportPath))
                    {
                        void OnError(Exception e)
                        {
                            Debug.LogException(e);
                        }

                        BuildManager.Build(exportPath, assetInfoManifest).Forget(OnError);
                    }
                    else
                    {
                        Debug.LogError("The export path is not set.");

                        var config = ExternalAssetConfig.Instance;

                        if (config != null)
                        {
                            Selection.activeObject = config;
                        }
                    }
                }
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }
    }
}