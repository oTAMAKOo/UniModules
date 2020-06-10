
using UnityEngine;
using UnityEditor;
using System;

namespace Modules.Devkit.AssetTuning
{
    public sealed class TextureTuningPrePostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override int GetPostprocessOrder() { return 51; }

        private void OnPreprocessTexture()
        {
            var assetTuneManager = AssetTuneManager.Instance;

            try
            {
                foreach (var tuner in assetTuneManager.AssetTuners)
                {
                    var textureTuner = tuner as TextureAssetTuner;

                    if (textureTuner == null) { continue; }

                    if (textureTuner.Validate(assetPath))
                    {
                        textureTuner.OnPreprocessTexture(assetPath, assetTuneManager.IsFirstImport(assetPath));
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
