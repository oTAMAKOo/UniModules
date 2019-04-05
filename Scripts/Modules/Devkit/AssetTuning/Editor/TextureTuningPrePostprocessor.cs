
using UnityEngine;
using UnityEditor;
using System;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public interface ITextureAssetTuner : IAssetTuner
    {
        void OnPreprocessTexture(string path, bool isFirstImport);
    }

    public class TextureTuningPrePostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override int GetPostprocessOrder()
        {
            return 51;
        }

        private void OnPreprocessTexture()
        {
            var assetTuner = AssetTuner.Instance;

            try
            {
                foreach (var tuner in assetTuner.AssetTuners)
                {
                    var textureTuner = tuner as ITextureAssetTuner;

                    if (textureTuner == null) { continue; }

                    if (textureTuner.Validate(assetPath))
                    {
                        textureTuner.OnPreprocessTexture(assetPath, assetTuner.IsFirstImport(assetPath));
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
