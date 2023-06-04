
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Modules.Devkit.AssetTuning.TextureAsset;

namespace Modules.Devkit.AssetTuning
{
    public class TextureAssetTuner : AssetTuner
    {
        //----- params -----

        private static readonly BuildTargetGroup[] DefaultTargetPlatforms =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Standalone,
        };

        //----- field -----

        //----- property -----

        /// <summary> 適用対象 </summary>
        protected virtual BuildTargetGroup[] Platforms
        {
            get { return DefaultTargetPlatforms; }
        }

        //----- method -----

        public override bool Validate(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            return textureImporter != null;
        }

        public override void OnAssetCreate(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null) { return; }

            OnFirstImport(textureImporter);

            textureImporter.SaveAndReimport();
        }

        public virtual void OnPreprocessTexture(string assetPath, bool isFirstImport)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null) { return; }

            if (isFirstImport)
            {
                OnFirstImport(textureImporter);
            }
            else
            {
                if (TextureConfig.Prefs.forceModifyOnImport)
                {
                    ModifyTextureSettings(textureImporter);
                }
            }
        }

        protected virtual void OnFirstImport(TextureImporter textureImporter)
        {
            if (textureImporter == null) { return; }

            var config = TextureConfig.Instance;

            if (config == null){ return; }

            var assetPath = textureImporter.assetPath;

            var textureData = config.DefaultData;

            if(!textureData.IsIgnoreTarget(assetPath))
            {
                TextureImporterModify.Modify(textureImporter, textureData, Platforms);
            }

            ModifyTextureSettings(textureImporter);
        }

        private void ModifyTextureSettings(TextureImporter textureImporter)
        {
            var config = TextureConfig.Instance;

            if (config == null){ return; }

            var assetPath = textureImporter.assetPath;

            var textureData = config.GetData(assetPath);

            TextureImporterModify.Modify(textureImporter, textureData, Platforms);

            OverrideCompressionFormat(textureImporter);
        }

        private void OverrideCompressionFormat(TextureImporter textureImporter)
        {
            foreach (var platform in Platforms)
            {
                var textureSettings = textureImporter.GetPlatformTextureSettings(platform.ToString());

                var format = GetOverrideCompressionFormat(textureImporter, platform, textureSettings.format);

                if (format != textureSettings.format)
                {
                    textureSettings.format = format;

                    textureImporter.SetPlatformTextureSettings(textureSettings);
                }
            }
        }

        protected virtual TextureImporterFormat GetOverrideCompressionFormat(TextureImporter textureImporter, BuildTargetGroup platform, TextureImporterFormat format)
        {
            switch (platform)
            {
                case BuildTargetGroup.Standalone:
                    {
                        if (textureImporter.textureType == TextureImporterType.NormalMap)
                        {
                            format = TextureImporterFormat.DXT5;
                        }
                        else
                        {
                            if (format == TextureImporterFormat.DXT5)
                            {
                                var hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();

                                // アルファ値なしの場合はDXT1に変更.
                                if (!hasAlpha)
                                {
                                    format = TextureImporterFormat.DXT1;
                                }
                            }
                        }
                    }
                    break;
            }

            return format;
        }
    }
}
