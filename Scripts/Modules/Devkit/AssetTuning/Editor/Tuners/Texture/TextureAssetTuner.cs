﻿
using System;
using UnityEngine;
using UnityEditor;
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
                if (TextureConfig.Prefs.changeSettingOnImport)
                {
                    SetTextureSettings(textureImporter, false);
                    SetTextureTypeSettings(textureImporter, false);
                    SetCompressionSettings(textureImporter, false);
                }
            }
        }

        protected virtual void SetTextureSettings(TextureImporter textureImporter, bool firstImport)
        {
            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = false;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.filterMode = FilterMode.Bilinear;

            if (textureImporter.textureType == TextureImporterType.Sprite)
            {
                SetSpriteSettings(textureImporter, firstImport);
            }
        }

        protected virtual void SetSpriteSettings(TextureImporter textureImporter, bool firstImport)
        {
            textureImporter.spriteImportMode = SpriteImportMode.Single;

            var settings = new TextureImporterSettings();

            textureImporter.ReadTextureSettings(settings);

            settings.textureShape = TextureImporterShape.Texture2D;
            settings.alphaIsTransparency = true;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;

            textureImporter.SetTextureSettings(settings);
        }

        protected virtual void OnFirstImport(TextureImporter textureImporter)
        {
            if (textureImporter == null) { return; }

            SetTextureSettings(textureImporter, true);

            SetTextureTypeSettings(textureImporter, true);

            SetCompressionSettings(textureImporter, true);
        }

        protected virtual void SetCompressionSettings(TextureImporter textureImporter, bool firstImport)
        {
            var config = TextureConfig.Instance;

            if (config == null) { return; }

            if (!IsFolderItem(textureImporter.assetPath, config.CompressFolders, config.IgnoreCompressFolders))
            {
                return;
            }

            for (var i = 0; i < Platforms.Length; i++)
            {
                var platform = Platforms[i];

                Func<TextureImporterPlatformSettings, TextureImporterPlatformSettings> update = settings =>
                {
                    SetPlatformCompressionSettings(ref settings, firstImport);

                    settings.format = GetPlatformCompressionType(textureImporter, platform);

                    return settings;
                };

                textureImporter.SetPlatformTextureSetting(platform, update);
            }
        }

        protected virtual bool SetTextureTypeSettings(TextureImporter textureImporter, bool firstImport)
        {
            var config = TextureConfig.Instance;

            if (config == null) { return false; }

            if (textureImporter.textureType != TextureImporterType.Default) { return false; }

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

        protected virtual void SetPlatformCompressionSettings(ref TextureImporterPlatformSettings platformSetting, bool firstImport)
        {
            platformSetting.overridden = true;
            platformSetting.androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings;

            if (firstImport)
            {
                platformSetting.compressionQuality = 50;
                platformSetting.textureCompression = TextureImporterCompression.Compressed;
            }
        }

        protected virtual TextureImporterFormat GetPlatformCompressionType(TextureImporter textureImporter, BuildTargetGroup platform)
        {
            var format = TextureImporterFormat.RGBA32;

            var hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();

            switch (platform)
            {
                case BuildTargetGroup.iOS:
                    format = TextureImporterFormat.ASTC_6x6;
                    break;

                case BuildTargetGroup.Android:
                    format = TextureImporterFormat.ASTC_6x6;
                    break;
                case BuildTargetGroup.Standalone:
                    {
                        if (textureImporter.textureType == TextureImporterType.NormalMap)
                        {
                            format = TextureImporterFormat.DXT5;
                        }
                        else
                        {
                            format = hasAlpha ? TextureImporterFormat.DXT5 : TextureImporterFormat.DXT1;
                        }
                    }
                    break;
            }

            return format;
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
