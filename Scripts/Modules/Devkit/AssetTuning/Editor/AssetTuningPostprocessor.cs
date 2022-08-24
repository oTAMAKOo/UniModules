
using UnityEngine;
using UnityEditor;
using System;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public sealed class AssetTuningPostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override int GetPostprocessOrder()
        {
            return 50;
        }

		/// <summary> アセットインポート前のコールバック. </summary>
		private void OnPreprocessAsset()
		{
			if (Application.isBatchMode){ return; }

			var assetTuneManager = AssetTuneManager.Instance;

			var assetTuners = assetTuneManager.AssetTuners;

			foreach (var tuner in assetTuners)
			{
				var assetPath = assetImporter.assetPath;

				if (!tuner.Validate(assetPath)) { continue; }

				tuner.OnPreprocessAsset(assetPath);
			}
		}

        /// <summary> アセットインポート完了後のコールバック. </summary>
        /// <param name="importedAssets"> インポートされたアセットのファイルパス。 </param>
        /// <param name="deletedAssets"> 削除されたアセットのファイルパス。 </param>
        /// <param name="movedAssets"> 移動されたアセットのファイルパス。 </param>
        /// <param name="movedFromPath"> 移動されたアセットの移動前のファイルパス。 </param>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {
			if (Application.isBatchMode){ return; }

            var assetTuneManager = AssetTuneManager.Instance;

            var assetTuners = assetTuneManager.AssetTuners;

            try
            {
                using (new AssetEditingScope())
                {
                    foreach (var tuner in assetTuners)
                    {
                        tuner.OnBeforePostprocessAsset();
                    }

                    foreach (var tuner in assetTuners)
                    {
                        foreach (var path in importedAssets)
                        {
                            if (!tuner.Validate(path)) { continue; }

                            if (assetTuneManager.IsFirstImport(path))
                            {
                                tuner.OnAssetCreate(path);
                            }

                            tuner.OnPostprocessAsset(path);
                        }

                        foreach (var path in deletedAssets)
                        {
                            if (!tuner.Validate(path)) { continue; }

                            tuner.OnAssetDelete(path);
                        }

                        for (var i = 0; i < movedAssets.Length; i++)
                        {
                            if (!tuner.Validate(movedAssets[i])) { continue; }

                            tuner.OnAssetMove(movedAssets[i], movedFromPath[i]);
                        }
                    }

                    foreach (var path in importedAssets)
                    {
                        assetTuneManager.FinishFirstImport(path);
                    }

                    foreach (var tuner in assetTuners)
                    {
                        tuner.OnAfterPostprocessAsset();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
