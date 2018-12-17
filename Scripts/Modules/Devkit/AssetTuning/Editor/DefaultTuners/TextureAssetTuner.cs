
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

        //----- field -----

        private MethodInfo getWidthAndHeightMethodInfo = null;

        //----- property -----

        /// <summary> 適用対象 </summary>
        protected abstract BuildTargetGroup[] Platforms { get; }

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

            var assetPath = PathUtility.ConvertPathSeparator(path);
            var pathSplit = assetPath.Split(PathUtility.PathSeparator);

            var size = GetPreImportTextureSize(textureImporter);
            
            if (IgnoreCompressionFolders.Any(x => pathSplit.Contains(x)))
            {
                SetDefaultSettings(textureImporter);
                return;
            }

            // ASTC / ETC2が使えるか(4の倍数なら圧縮設定).
            var isMultipleOf4 = IsMultipleOf4(size.x) && IsMultipleOf4(size.y);

            if (!isMultipleOf4)
            {
                SetDefaultSettings(textureImporter);
                return;
            }

            ApplyCompressionSettings(textureImporter, (int)size.x, (int)size.y);
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
        }

        protected virtual void ApplyCompressionSettings(TextureImporter textureImporter, int width, int height)
        {
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
            var compressionType = TextureImporterFormat.RGBA32;

            var hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();

            switch (platform)
            {
                case BuildTargetGroup.Android:
                    compressionType = hasAlpha ? TextureImporterFormat.ETC2_RGBA8Crunched : TextureImporterFormat.ETC2_RGB4;
                    break;

                case BuildTargetGroup.iOS:
                    compressionType = hasAlpha ? TextureImporterFormat.ASTC_RGBA_4x4 : TextureImporterFormat.ASTC_RGB_4x4;
                    break;
            }

            return compressionType;
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
