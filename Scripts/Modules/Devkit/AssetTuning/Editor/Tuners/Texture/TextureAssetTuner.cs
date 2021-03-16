
using UnityEditor;
using System;
using System.Linq;
using Extensions;

using Object = UnityEngine.Object;

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
                SetTextureTypeSettings(textureImporter);
                SetCompressionSettings(textureImporter);
            }
        }

        protected virtual void OnFirstImport(TextureImporter textureImporter)
        {
            if (textureImporter == null) { return; }

            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = false;
            textureImporter.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterSettings();

            textureImporter.ReadTextureSettings(settings);

            settings.spriteGenerateFallbackPhysicsShape = false;

            textureImporter.SetTextureSettings(settings);

            SetTextureTypeSettings(textureImporter);

            SetCompressionSettings(textureImporter);
        }

        protected virtual void SetCompressionSettings(TextureImporter textureImporter)
        {
            var config = TextureAssetTunerConfig.Instance;

            if (config == null) { return; }

            if (!IsFolderItem(textureImporter.assetPath, config.CompressFolders, config.IgnoreCompressFolders))
            {
                return;
            }

            foreach (var platform in Platforms)
            {
                Func<TextureImporterPlatformSettings, TextureImporterPlatformSettings> update = settings =>
                {
                    settings.overridden = true;
                    settings.compressionQuality = 50;
                    settings.textureCompression = TextureImporterCompression.Compressed;
                    settings.androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings;
                    settings.format = GetPlatformCompressionType(textureImporter, platform);

                    return settings;
                };

                textureImporter.SetPlatformTextureSetting(platform, update);
            }
        }

        protected virtual bool SetTextureTypeSettings(TextureImporter textureImporter)
        {
            var config = TextureAssetTunerConfig.Instance;

            if (config == null) { return false; }

            var isTarget = false;

            var parts = textureImporter.assetPath.Split(PathUtility.PathSeparator);

            isTarget |= parts.Any(x => config.SpriteFolderNames.Contains(x));

            isTarget |= IsFolderItem(textureImporter.assetPath, config.SpriteFolders, config.IgnoreSpriteFolders);

            if (isTarget)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
            }

            return isTarget;
        }

        protected virtual TextureImporterFormat GetPlatformCompressionType(TextureImporter textureImporter, BuildTargetGroup platform)
        {
            var format = TextureImporterFormat.RGBA32;

            var hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();

            switch (platform)
            {
                case BuildTargetGroup.iOS:
                    format = TextureImporterFormat.ASTC_RGB_4x4;
                    break;

                case BuildTargetGroup.Android:
                    format = TextureImporterFormat.ASTC_RGB_4x4;
                    break;
                case BuildTargetGroup.Standalone:
                    format = hasAlpha ? TextureImporterFormat.DXT5 : TextureImporterFormat.DXT1;
                    break;
            }

            return format;
		}

        protected void SetDefaultSettings(TextureImporter textureImporter)
        {
            foreach (var platform in Platforms)
            {
                var platformTextureSetting = textureImporter.GetPlatformTextureSettings(platform.ToString());

                Func<TextureImporterPlatformSettings, TextureImporterPlatformSettings> update = settings =>
                {
                    platformTextureSetting.overridden = false;

                    return platformTextureSetting;
                };

                textureImporter.SetPlatformTextureSetting(platform, update);
            }
        }

        public static bool IsFolderItem(string assetPath, Object[] folders, string[] ignoreFolders)
        {
            assetPath = PathUtility.ConvertPathSeparator(assetPath);

            ignoreFolders = ignoreFolders.Select(x => PathUtility.ConvertPathSeparator(x)).ToArray();

            var targetPaths = folders.Where(x => x != null).Select(x => AssetDatabase.GetAssetPath(x));

            var pathSeparatorStr = PathUtility.PathSeparator.ToString();

            foreach (var targetPath in targetPaths)
            {
                var path = PathUtility.ConvertPathSeparator(targetPath);

                if (assetPath.StartsWith(path + pathSeparatorStr))
                {
                    // フォルダパスで除外.

                    var ignoreFolderPaths = ignoreFolders.Where(x => x.EndsWith(pathSeparatorStr)).ToArray();

                    if (ignoreFolderPaths.Any(x => assetPath.Contains(x))) { continue; }

                    // フォルダ名で除外.

                    var parts = assetPath.Substring(path.Length).Split(PathUtility.PathSeparator);

                    if (parts.Any(x => ignoreFolders.Contains(x))) { continue; }

                    return true;
                }
            }

            return false;
        }
    }
}
