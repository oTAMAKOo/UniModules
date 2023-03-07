
using UnityEngine;
using UnityEditor;
using Extensions;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
    public static class TextureImporterModify
    {
		public static void Modify(TextureImporter importer, TextureData textureData, BuildTargetGroup[] platforms)
		{
			if (textureData == null){ return; }

			importer.textureType = textureData.textureType;

			var textureSettings = new TextureImporterSettings();

			importer.ReadTextureSettings(textureSettings);

			//---------------------
			// Texture 
			//---------------------

			textureSettings.textureShape = textureData.textureShape;
			textureSettings.sRGBTexture = textureData.sRGBTexture;
			textureSettings.alphaSource = textureData.alphaSource;
			textureSettings.alphaIsTransparency = textureData.alphaIsTransparency;
			textureSettings.ignorePngGamma = textureData.ignorePngGamma;
			textureSettings.npotScale = textureData.npotScale;
			textureSettings.readable = textureData.readable;
			textureSettings.streamingMipmaps = textureData.streamingMipmaps;
			textureSettings.vtOnly = textureData.vtOnly;
			textureSettings.mipmapEnabled = textureData.mipmapEnabled;
			textureSettings.borderMipmap = textureData.borderMipmap;
			textureSettings.mipmapFilter = textureData.mipmapFilter;
			textureSettings.mipMapsPreserveCoverage = textureData.mipMapsPreserveCoverage;
			textureSettings.alphaTestReferenceValue = textureData.alphaTestReferenceValue;
			textureSettings.fadeOut = textureData.fadeOut;
			textureSettings.mipmapFadeDistanceStart = textureData.mipMapFadeDistanceStart;
			textureSettings.mipmapFadeDistanceEnd = textureData.mipMapFadeDistanceEnd;

			textureSettings.wrapMode = textureData.wrapMode;
			textureSettings.filterMode = textureData.filterMode;
			textureSettings.aniso = textureData.anisoLevel;

			//---------------------
			// BumpMap
			//---------------------

			textureSettings.convertToNormalMap = textureData.convertToNormalMap;
			textureSettings.normalMapFilter = textureData.normalMapFilter;
			textureSettings.heightmapScale = textureData.heightmapScale;

			//---------------------
			// Sprite 
			//---------------------

			textureSettings.spriteMode = (int)textureData.spriteMode;
			textureSettings.spriteMeshType = textureData.meshType;
			textureSettings.spritePixelsPerUnit = textureData.spritePixelsPerUnit;
			textureSettings.spritePivot = new Vector2(textureData.spritePivotX, textureData.spritePivotY);
			textureSettings.spriteAlignment = (int)textureData.spriteAlignment;
			textureSettings.spriteExtrude = textureData.spriteExtrude;
			textureSettings.spriteGenerateFallbackPhysicsShape = textureData.spriteGenerateFallbackPhysicsShape;

			//--------------------

			importer.SetTextureSettings(textureSettings);

			//---------------------
			// Platform
			//---------------------

			ModifyPlatformSettings(importer, textureData, platforms);
		}

		private static void ModifyPlatformSettings(TextureImporter importer, TextureData textureData, BuildTargetGroup[] platforms)
		{
			// Default.
			{
				var platformSettings = importer.GetDefaultPlatformTextureSettings();
				var platformData = textureData.GetDefaultPlatformData();

				ModifyPlatformSettings(importer, platformSettings, platformData, true);
			}

			// BuildTargetGroup.
			foreach (var platform in platforms)
			{
				var platformSettings = importer.GetPlatformTextureSettings(platform.ToString());
				var platformData = textureData.GetPlatformData(platform);

				ModifyPlatformSettings(importer, platformSettings, platformData, false);
			}
		}

		private static void ModifyPlatformSettings(TextureImporter importer, TextureImporterPlatformSettings platformSettings, PlatformData platformData, bool isDefault)
		{
			var editSettings = new TextureImporterPlatformSettings();

			platformSettings.CopyTo(editSettings);

			if (!isDefault)
			{
				editSettings.overridden = platformData.isOverride;
			}

			editSettings.format = platformData.format;
			editSettings.compressionQuality = platformData.compressionQuality;
			editSettings.maxTextureSize = platformData.maxSize;
			editSettings.resizeAlgorithm = platformData.resizeAlgorithm;

			if (!platformSettings.Equals(editSettings))
			{
				importer.SetPlatformTextureSettings(editSettings);
			}
		}
	}
}