
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;

using Object = UnityEngine.Object;

namespace Modules.AtlasTexture
{
    public static class AtlasTextureGenerator
    {
        public static AtlasTexture Generate(Object activeObject, string assetName)
        {
            var directory = string.Empty;

            if (Selection.activeObject != null)
            {
                var activeObjectPath = AssetDatabase.GetAssetPath(Selection.activeObject);

                if (UnityEditorUtility.IsFolder(activeObjectPath))
                {
                    directory = activeObjectPath;
                }
                else
                {
                    directory = Path.GetDirectoryName(activeObjectPath);
                }
            }
            else
            {
                directory = UnityPathUtility.AssetsFolder;
            }

            var assetPath = PathUtility.Combine(directory, assetName);

            if (File.Exists(assetPath))
            {
                Debug.LogWarningFormat("File already exists.\n{0}", assetPath);

                return null;
            }

            return Generate(assetPath);
        }

        public static AtlasTexture Generate(string assetPath)
        {
            var path = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var atlasTexture = CreateAtlasTexture(assetPath);

            if (atlasTexture == null) { return null; }

            var spriteAtlas = CreateSpriteAtlas(assetPath);

            if (spriteAtlas == null) { return null; }

            Reflection.SetPrivateField(atlasTexture, "spriteAtlas", spriteAtlas);
            
            return atlasTexture;
        }

        private static AtlasTexture CreateAtlasTexture(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return null; }

            return ScriptableObjectGenerator.Generate<AtlasTexture>(assetPath);
        }

        public static SpriteAtlas CreateSpriteAtlas(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return null; }

            string yaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!687078895 &4343727234628468602
SpriteAtlas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: #ATLASNAME#
  m_EditorData:
    serializedVersion: 2
    textureSettings:
      serializedVersion: 2
      anisoLevel: 1
      compressionQuality: 50
      maxTextureSize: 1024
      textureCompression: 0
      filterMode: 1
      generateMipMaps: 0
      readable: 0
      crunchedCompression: 0
      sRGB: 1
    platformSettings:
    - serializedVersion: 2
      m_BuildTarget: Android
      m_MaxTextureSize: 1024
      m_ResizeAlgorithm: 0
      m_TextureFormat: 54
      m_TextureCompression: 1
      m_CompressionQuality: 50
      m_CrunchedCompression: 0
      m_AllowsAlphaSplitting: 0
      m_Overridden: 1
      m_AndroidETC2FallbackOverride: 0
    - serializedVersion: 2
      m_BuildTarget: iPhone
      m_MaxTextureSize: 1024
      m_ResizeAlgorithm: 0
      m_TextureFormat: 54
      m_TextureCompression: 1
      m_CompressionQuality: 50
      m_CrunchedCompression: 0
      m_AllowsAlphaSplitting: 0
      m_Overridden: 1
      m_AndroidETC2FallbackOverride: 0
    - serializedVersion: 2
      m_BuildTarget: DefaultTexturePlatform
      m_MaxTextureSize: 1024
      m_ResizeAlgorithm: 0
      m_TextureFormat: -1
      m_TextureCompression: 1
      m_CompressionQuality: 50
      m_CrunchedCompression: 0
      m_AllowsAlphaSplitting: 0
      m_Overridden: 0
      m_AndroidETC2FallbackOverride: 0
    packingSettings:
      serializedVersion: 2
      padding: 2
      blockOffset: 1
      allowAlphaSplitting: 0
      enableRotation: 0
      enableTightPacking: 0
    variantMultiplier: 1
    packables: []
    totalSpriteSurfaceArea: 0
    bindAsDefault: 0
  m_MasterAtlas: {fileID: 0}
  m_PackedSprites: []
  m_PackedSpriteNamesToIndex: []
  m_Tag: New Sprite Atlas
  m_IsVariant: 0
";

            assetPath = Path.ChangeExtension(assetPath, ".spriteatlas");

            var filePath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

            if (File.Exists(assetPath))
            {
                File.Delete(assetPath);
                AssetDatabase.Refresh();
            }

            var atlasName = Path.GetFileNameWithoutExtension(assetPath);

            using (var fileStream = new FileStream(filePath, FileMode.CreateNew))
            {
                yaml = yaml.Replace("#ATLASNAME#", atlasName);

                var bytes = new UTF8Encoding().GetBytes(yaml);

                fileStream.Write(bytes, 0, bytes.Length);
            }

            AssetDatabase.Refresh();

            return AssetDatabase.LoadMainAssetAtPath(assetPath) as SpriteAtlas;
        }
    }
}
