
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Extensions;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
	[Serializable]
	public sealed class PlatformData
	{
		[SerializeField]
		public bool isOverride = false;

		[SerializeField]
		public int maxSize = 2048;

		[SerializeField]
		public TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

		[SerializeField]
		public TextureImporterFormat format = TextureImporterFormat.Automatic;

		[SerializeField]
		public int compressionQuality = 50;
	}

	[Serializable]
	public sealed class TextureData
	{
		//----- params -----

		//----- field -----

		[SerializeField]
		public string folderGuid = null;

		//-------------------------------
		// Texture
		//-------------------------------

		[SerializeField]
		public TextureImporterType textureType = TextureImporterType.Default;

		[SerializeField]
		public TextureImporterShape textureShape = TextureImporterShape.Texture2D;
		[SerializeField]
		public bool sRGBTexture = true;
		[SerializeField]
		public TextureImporterAlphaSource alphaSource = TextureImporterAlphaSource.FromInput;
		[SerializeField]
		public bool alphaIsTransparency = true;
		[SerializeField]
		public bool ignorePngGamma = false;

		[SerializeField]
		public TextureImporterNPOTScale npotScale = TextureImporterNPOTScale.None;
		[SerializeField]
		public bool readable = false;
		[SerializeField]
		public bool streamingMipmaps = false;
		[SerializeField]
		public bool vtOnly = false;
		[SerializeField]
		public bool mipmapEnabled = false;
		[SerializeField]
		public bool borderMipmap = false;
		[SerializeField]
		public TextureImporterMipFilter mipmapFilter = TextureImporterMipFilter.BoxFilter;
		[SerializeField]
		public bool mipMapsPreserveCoverage = false;
		[SerializeField]
		public float alphaTestReferenceValue = 0.5f;
		[SerializeField]
		public bool fadeOut = false;
		[SerializeField]
		public int mipMapFadeDistanceStart = 1;
		[SerializeField]
		public int mipMapFadeDistanceEnd = 3;

		//-------------------------------
		// BumpMap
		//-------------------------------

		[SerializeField]
		public bool convertToNormalMap = false;
		[SerializeField]
		public float heightmapScale = 0.25f;
		[SerializeField]
		public TextureImporterNormalFilter normalMapFilter = TextureImporterNormalFilter.Standard;

		//-------------------------------
		// Sprite
		//-------------------------------

		[SerializeField]
		public SpriteImportMode spriteMode = SpriteImportMode.Single;
		[SerializeField]
		public SpriteMeshType meshType = SpriteMeshType.FullRect;
		[SerializeField]
		public float spritePixelsPerUnit = 100f;
		[SerializeField]
		public SpriteAlignment spriteAlignment = SpriteAlignment.Center;
		[SerializeField]
		public float spritePivotX = 0f;
		[SerializeField]
		public float spritePivotY = 0f;
		[SerializeField]
		public uint spriteExtrude = 1;
		[SerializeField]
		public bool spriteGenerateFallbackPhysicsShape = true;

		//-------------------------------
		// Mode
		//-------------------------------

		[SerializeField]
		public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
		[SerializeField]
		public FilterMode filterMode = FilterMode.Bilinear;
		[SerializeField]
		public int anisoLevel = 1;

		//-------------------------------
		// Platform
		//-------------------------------

		[SerializeField]
		public PlatformData defaultPlatform = new PlatformData();
		[SerializeField]
		public PlatformData iosPlatform = new PlatformData();
		[SerializeField]
		public PlatformData androidPlatform = new PlatformData();
		[SerializeField]
		public PlatformData standalonePlatform = new PlatformData();

		//-------------------------------
		// Options
		//-------------------------------

		[SerializeField]
		public string[] ignoreFolders = new string[0];
		[SerializeField]
		public string[] ignoreFolderNames = new string[0];

		//----- property -----

		//----- method -----

		public PlatformData GetDefaultPlatformData()
		{
			return defaultPlatform;
		}

		public PlatformData GetPlatformData(BuildTargetGroup buildTargetGroup)
		{
			switch (buildTargetGroup)
			{
				case BuildTargetGroup.iOS:        return iosPlatform;
				case BuildTargetGroup.Android:    return androidPlatform;
				case BuildTargetGroup.Standalone: return standalonePlatform;
			}

			return null;
		}

		public bool IsIgnoreTarget(string assetPath)
		{
			// Ignore Folder.
			{
				var elements = ignoreFolders
					.Where(x => x != null)
					.Select(x => AssetDatabase.GUIDToAssetPath(x))
					.ToArray();

				foreach (var element in elements)
				{
					if (assetPath.StartsWith(element)){ return true; }
				}
			}

			// Ignore FolderName. 
			{
				var elements = ignoreFolderNames
					.Select(x => x.Trim())
					.Where(x => !string.IsNullOrEmpty(x))
					.ToArray();

				foreach (var element in elements)
				{
					if (assetPath.Contains(element)){ return true; }
				}
			}

			return false;
		}
	}
}