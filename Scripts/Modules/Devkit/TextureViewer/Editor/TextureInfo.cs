
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

        private string textureSizeText = null;

        private string fileSizeText = null;
        
        private string importWarning = null;
        
        //----- property -----

        public int Id { get; private set; }

        public string Guid { get; private set; }

        public string AssetPath { get; private set; }

        public string TextureName { get; private set; }

        public float NameWidth { get; private set; }
        
        public long TextureSize { get; private set; }

        public long FileSize { get; private set; }

        public TextureImporter TextureImporter { get; private set; }

        public bool HasWarning
        {
            get
            {
                return !string.IsNullOrEmpty(importWarning);
            }
        }
                
        //----- method -----

        public TextureInfo(int id, string guid, string assetPath)
        {
            Id = id;

            Guid = guid;

            AssetPath = assetPath;

            TextureName = Path.GetFileNameWithoutExtension(AssetPath);

            TextureImporter = AssetImporter.GetAtPath(AssetPath) as TextureImporter;

            importWarning = TextureImporter.GetImportWarning();

            var filePath = UnityPathUtility.ConvertAssetPathToFullPath(AssetPath);
            
            FileSize = new FileInfo(filePath).Length;

            var labelLayoutSize = EditorStyles.label.CalcSize(new GUIContent(TextureName));

            NameWidth = labelLayoutSize.x;
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

                TextureSize = texture.width * texture.height;
            }

            return texture;
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

        public string GetFileSizeText()
        {
            if (string.IsNullOrEmpty(fileSizeText))
            {
                if (1024.0f * 1024.0f * 1024.0f <= FileSize) // GB
                {
                    fileSizeText = string.Format("{0:F1}GB", FileSize / (1024.0f * 1024.0f * 1024.0f));
                }
                else if (1024.0f * 1024.0f <= FileSize) // MB
                {
                    fileSizeText = string.Format("{0:F1}MB", FileSize / (1024.0f * 1024.0f));
                }
                else // KB
                {
                    fileSizeText = string.Format("{0:F1}KB", FileSize / 1024.0f);
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

        public bool IsMatch(string[] keywords)
        {
            var result = false;

            result |= AssetPath.IsMatch(keywords);

            return result;
        }
    }
}