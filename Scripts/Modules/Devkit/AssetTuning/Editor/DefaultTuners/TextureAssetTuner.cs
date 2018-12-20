
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using Extensions;

namespace Modules.Devkit.AssetTuning
{
    public abstract class TextureAssetTuner : ITextureAssetTuner
    {
        //----- params -----

        private static readonly BuildTargetGroup[] DefaultTargetPlatforms =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
        };

        //----- field -----

        private MethodInfo getWidthAndHeightMethodInfo = null;

        //----- property -----

        public int Priority { get { return 25; } }

        /// <summary> 適用対象 </summary>
        protected virtual BuildTargetGroup[] Platforms
        {
            get { return DefaultTargetPlatforms; }
        }

        // 圧縮設定を適用しないフォルダ名.
        protected abstract string[] IgnoreCompressionFolders { get; }

        //----- method -----
       
        public bool Validate(string path)
        {
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            return textureImporter != null;
        }

        public void OnAssetCreate(string path)
        {
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (textureImporter == null) { return; }

            OnFirstImport(textureImporter);

            textureImporter.SaveAndReimport();
        }

        public virtual void OnAssetImport(string path) {}

        public virtual void OnAssetDelete(string path) {}

        public virtual void OnAssetMove(string path, string from) {}

        public virtual void OnPreprocessTexture(string path, bool isFirstImport)
        {
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (textureImporter == null) { return; }

            if (isFirstImport)
            {
                OnFirstImport(textureImporter);
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

            SetCompressionSettings(textureImporter);
        }

        protected virtual void SetCompressionSettings(TextureImporter textureImporter)
        {
            var pathSplit = textureImporter.assetPath.Split(PathUtility.PathSeparator);

            var size = GetPreImportTextureSize(textureImporter);

            if (IgnoreCompressionFolders.Any(x => pathSplit.Contains(x)))
            {
                SetDefaultSettings(textureImporter);
                return;
            }

            // ブロックが使えるか(4の倍数なら圧縮設定).
            var isMultipleOf4 = IsMultipleOf4(size.x) && IsMultipleOf4(size.y);

            if (!isMultipleOf4)
            {
                SetDefaultSettings(textureImporter);
                return;
            }

            foreach (var platform in Platforms)
            {
                var platformTextureSetting = textureImporter.GetPlatformTextureSettings(platform.ToString());

                var settings = new TextureImporterPlatformSettings();

                platformTextureSetting.CopyTo(settings);

                settings.overridden = true;
                settings.compressionQuality = (int)UnityEngine.TextureCompressionQuality.Normal;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings;

                settings.format = GetPlatformCompressionType(textureImporter, platform);

                if (!platformTextureSetting.Equals(settings))
                {
                    textureImporter.SetPlatformTextureSettings(settings);
                }
            }
        }

        protected virtual TextureImporterFormat GetPlatformCompressionType(TextureImporter textureImporter, BuildTargetGroup platform)
        {
            TextureImporterFormat format = TextureImporterFormat.RGBA32;

            var hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();

            switch (platform)
            {
                case BuildTargetGroup.iOS:
                    format = hasAlpha ? TextureImporterFormat.ASTC_RGBA_4x4 : TextureImporterFormat.ASTC_RGB_4x4;
                    break;

                case BuildTargetGroup.Android:
                    format = hasAlpha ? TextureImporterFormat.ASTC_RGBA_4x4 : TextureImporterFormat.ASTC_RGB_4x4;
                    break;
            }

            return format;
		}

        protected void SetDefaultSettings(TextureImporter textureImporter)
        {
            foreach (var platform in Platforms)
            {
                var platformTextureSetting = textureImporter.GetPlatformTextureSettings(platform.ToString());

                var settings = new TextureImporterPlatformSettings();

                platformTextureSetting.CopyTo(settings);

                settings.overridden = false;

                if (!platformTextureSetting.Equals(settings))
                {
                    textureImporter.SetPlatformTextureSettings(settings);
                }
            }
        }

        protected Vector2 GetPreImportTextureSize(TextureImporter importer)
        {
            // ※ TextureImporter.GetWidthAndHeightは非公開メソッドなのでリフレクションで無理矢理呼び出し.

            if (getWidthAndHeightMethodInfo == null)
            {
                getWidthAndHeightMethodInfo = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var args = new object[] { 0, 0 };

            getWidthAndHeightMethodInfo.Invoke(importer, args);

            return new Vector2((int)args[0], (int)args[1]);
		}

		protected bool IsMultipleOf4(float value)
        {
            return value % 4 == 0;
        }
	}
}
