
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace Extensions
{
    public static class TextureImporterExtensions
    {
        private static readonly string[] BlockCompressFormatTable = new string[]
        {
            "ASTC_", "ASTC_RGB_", "ASTC_RGBA_", "ASTC_HDR_", "DXT",
        };

        private static MethodInfo getWidthAndHeightMethodInfo = null;

        /// <summary> 圧縮設定がブロック圧縮か </summary>
        public static bool IsBlockCompress(this TextureImporter importer, BuildTargetGroup platform)
        {
            if (importer == null){ return false; }

            var setting = importer.GetPlatformTextureSetting(platform);

            var format = setting.format.ToString();

            return BlockCompressFormatTable.Any(x => format.StartsWith(x));
        }

        /// <summary> テクスチャ設定取得 </summary>
        public static TextureImporterPlatformSettings GetPlatformTextureSetting(this TextureImporter importer, BuildTargetGroup platform)
        {
            if (importer == null){ return null; }

            var settings = new TextureImporterPlatformSettings();

            var platformTextureSetting = importer.GetPlatformTextureSettings(platform.ToString());

            platformTextureSetting.CopyTo(settings);

            return settings;
        }

        /// <summary> テクスチャの設定変更 </summary>
        public static void SetPlatformTextureSetting(this TextureImporter importer, BuildTargetGroup platform,
                                                     Func<TextureImporterPlatformSettings, TextureImporterPlatformSettings> update)
        {
            var settings = importer.GetPlatformTextureSetting(platform);

            var prevSettings = new TextureImporterPlatformSettings();

            settings.CopyTo(prevSettings);

            var newSettings = update(settings);

            if (!prevSettings.Equals(newSettings))
            {
                importer.SetPlatformTextureSettings(newSettings);
            }
        }

        /// <summary> テクスチャサイズ取得 </summary>
        public static Vector2 GetPreImportTextureSize(this TextureImporter importer)
        {
            // ※ TextureImporter.GetWidthAndHeightは非公開メソッドなので無理矢理呼び出し.     

            var args = new object[] { 0, 0 };

            if (getWidthAndHeightMethodInfo == null)
            {
                getWidthAndHeightMethodInfo = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            getWidthAndHeightMethodInfo.Invoke(importer, args);

            return new Vector2((int)args[0], (int)args[1]);
        }
    }
}
