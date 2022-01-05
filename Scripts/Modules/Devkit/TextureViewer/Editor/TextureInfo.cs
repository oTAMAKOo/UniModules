
using UnityEngine;
using UnityEditor;
using System.IO;
using Extensions;

namespace Modules.Devkit.TextureViewer
{
    public sealed class TextureInfo
    {
        //----- params -----

        //----- field -----

        private Texture icon = null;

        private Texture texture = null;

        private TextureImporter textureImporter = null;

        private string textureName = null;

        private string textureSizeText = null;

        private string fileSizeText = null;

        private float? nameWidth = null;
        
        private string importWarning = null;
        private long? fileSize = null;

        private long? textureSize = null;

        //----- property -----

        public int Id { get; private set; }

        public string Guid { get; private set; }

        public string AssetPath { get; private set; }

        public TextureImporter TextureImporter
        {
            get
            {
                return textureImporter ?? (textureImporter = AssetImporter.GetAtPath(AssetPath) as TextureImporter);
            }
        }

        //----- method -----

        public TextureInfo(int id, string guid, string assetPath)
        {
            Id = id;

            Guid = guid;

            AssetPath = assetPath;
        }

        public Texture GetTextureIcon()
        {
            if (icon == null)
            {
                icon = AssetDatabase.GetCachedIcon(AssetPath);
            }

            return icon;
        }

        public Texture GetTexture()
        {
            if (texture == null)
            {
                texture = AssetDatabase.LoadMainAssetAtPath(AssetPath) as Texture;
            }

            return texture;
        }

        public long GetTextureSize()
        {
            if (!textureSize.HasValue)
            {
                var texture = GetTexture();

                textureSize = texture.width * texture.height;
            }

            return textureSize.Value;
        }

        public string GetTextureSizeText()
        {
            var texture = GetTexture();

            if (string.IsNullOrEmpty(textureSizeText))
            {
                textureSizeText = string.Format("{0}x{1}", texture.width, texture.height);
            }

            return textureSizeText;
        }

        public string GetTextureName()
        {
            return textureName ?? (textureName = Path.GetFileNameWithoutExtension(AssetPath));
        }

        public float GetNameWidth()
        {
            if (!nameWidth.HasValue)
            {
                var name = GetTextureName();
                var labelLayoutSize = EditorStyles.label.CalcSize(new GUIContent(name));

                nameWidth = labelLayoutSize.x;
            }

            return nameWidth.Value;
        }

        public string GetFileSizeText()
        {
            if (string.IsNullOrEmpty(fileSizeText))
            {
                var size = GetFileSize();

                if (1024.0f * 1024.0f * 1024.0f <= size) // GB
                {
                    fileSizeText = string.Format("{0:F1}GB", size / (1024.0f * 1024.0f * 1024.0f));
                }
                else if (1024.0f * 1024.0f <= size) // MB
                {
                    fileSizeText = string.Format("{0:F1}MB", size / (1024.0f * 1024.0f));
                }
                else // KB
                {
                    fileSizeText = string.Format("{0:F1}KB", size / 1024.0f);
                }
            }

            return fileSizeText;
        }

        public string GetFormatText(BuildTargetGroup platform)
        {
            var textureSettings = TextureImporter.GetPlatformTextureSettings(platform.ToString());

            return textureSettings.format.ToString();
        }

        public float GetFormatTextWidth(BuildTargetGroup platform)
        {
            var formatText = GetFormatText(platform);

            var formatTextSize = EditorStyles.label.CalcSize(new GUIContent(formatText));

            return formatTextSize.x;
        }

        public bool GetCompressOverridden(BuildTargetGroup platform)
        {
            var textureSettings = TextureImporter.GetPlatformTextureSettings(platform.ToString());

            return textureSettings.overridden;
        }

        public int GetMaxTextureSize(BuildTargetGroup platform)
        {
            var textureSettings = TextureImporter.GetPlatformTextureSettings(platform.ToString());

            return textureSettings.maxTextureSize;
        }

        public string GetImportWarning()
        {
            if (importWarning == null)
            {
                importWarning = TextureImporter.GetImportWarning();
            }

            return importWarning;
        }

        public bool HasWarning()
        {
            var warning = GetImportWarning();

            return !string.IsNullOrEmpty(warning);
        }

        public long GetFileSize()
        {
            if (!fileSize.HasValue)
            {
                var filePath = UnityPathUtility.ConvertAssetPathToFullPath(AssetPath);

                fileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;
            }

            return fileSize.Value;
        }

        public bool IsMatch(string[] keywords)
        {
            var result = false;

            result |= AssetPath.IsMatch(keywords);

            return result;
        }
    }
}