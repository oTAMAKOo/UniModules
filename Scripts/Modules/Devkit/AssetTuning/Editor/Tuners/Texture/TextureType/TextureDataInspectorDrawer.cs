
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Inspector;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
    public abstract class TextureDataInspectorDrawer : LifetimeDisposable
    {
        //----- params -----

		private sealed class FolderNameRegisterScrollView : RegisterScrollView<string>
		{
			protected override string CreateNewContent()
			{
				return string.Empty;
			}

			protected override string DrawContent(Rect rect, int index, string content)
			{
				return EditorGUI.DelayedTextField(rect, content);
			}
		}

        //----- field -----

		protected TextureData textureData = null;

		private Dictionary<PlatformType, GUIContent> iconContents = null;

		private FolderRegisterScrollView ignoreFolderView = null;
		private FolderNameRegisterScrollView ignoreFolderNameView = null;

		private int tabSelection = 0;

        //----- property -----

        //----- method -----

		public TextureDataInspectorDrawer(TextureData textureData)
		{
			this.textureData = textureData;

			textureData.textureShape = TextureImporterShape.Texture2D;

			//------ IgnoreFolderView ------

			ignoreFolderView = new FolderRegisterScrollView("Ignore Folders", $"{GetType().FullName}-IgnoreFolderView");

			ignoreFolderView.RemoveChildrenFolder = true;

			ignoreFolderView.OnUpdateContentsAsObservable()
				.Subscribe(x => textureData.ignoreFolders = x.Select(y => UnityEditorUtility.GetAssetGUID(y.asset)).ToArray())
				.AddTo(Disposable);

			var ignoreFolderGuids = textureData.ignoreFolders;

			ignoreFolderView.SetContents(ignoreFolderGuids);

			//------ IgnoreFolderNameView ------

			ignoreFolderNameView = new FolderNameRegisterScrollView();

			ignoreFolderNameView.OnUpdateContentsAsObservable()
				.Subscribe(x => textureData.ignoreFolderNames = x)
				.AddTo(Disposable);

			var spriteFolderGuids = textureData.ignoreFolderNames;

			ignoreFolderNameView.SetContents(spriteFolderGuids);
		}

		public void DrawInspectorGUI()
		{
			var textureTypeName = textureData.textureType.ToString();

			EditorLayoutTools.ContentTitle($"Texture ({textureTypeName})");

			using (new ContentsScope())
			{
				DrawTextureSettingGUI();
			}

			DrawPlatformSettingGUI();

			DrawOptionSettingGUI();
		}

		private void DrawPlatformSettingGUI()
		{
			LoadIcons();

			EditorLayoutTools.ContentTitle("Platform");

			using (new ContentsScope())
			{
				var tabItems = new Dictionary<PlatformType, PlatformData>
				{
					{ PlatformType.Default, textureData.defaultPlatform },
					{ PlatformType.Standalone, textureData.standalonePlatform },
					{ PlatformType.Android, textureData.androidPlatform },
					{ PlatformType.iOS, textureData.iosPlatform },
				};

				var tabContents = tabItems.Select(x => iconContents.GetValueOrDefault(x.Key)).ToArray();

				tabSelection = GUILayout.Toolbar(tabSelection, tabContents);

				EditorGUILayout.Separator();

				var item = tabItems.ElementAtOrDefault(tabSelection);

				if (!item.IsDefault())
				{
					DrawPlatformSettingDetailGUI(item.Key, item.Value);
				}
			}
		}

		public void DrawPlatformSettingDetailGUI(PlatformType platformType, PlatformData platformData)
		{
			// isOverride.

			var isOverride = true;

			if (platformType != PlatformType.Default)
			{
				platformData.isOverride = EditorGUILayout.Toggle($"Override For {platformType}", platformData.isOverride);

				isOverride = platformData.isOverride;

				GUILayout.Space(2f);
			}

			using (new DisableScope(!isOverride))
			{
				// MaxSize.

				var sizeTable = new int[]{ 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };

				var maxSizeLabels = sizeTable.Select(x => x.ToString()).ToArray();
				var maxSizeIndex = sizeTable.IndexOf(x => x == platformData.maxSize); 

				EditorGUI.BeginChangeCheck();

				maxSizeIndex = EditorGUILayout.Popup("MaxSize", maxSizeIndex, maxSizeLabels);

				if (EditorGUI.EndChangeCheck())
				{
					platformData.maxSize = sizeTable[maxSizeIndex];
				}

				GUILayout.Space(2f);

				// ResizeAlgorithm.

				platformData.resizeAlgorithm = (TextureResizeAlgorithm)EditorGUILayout.EnumPopup("ResizeAlgorithm", platformData.resizeAlgorithm);

				// Format.

				platformData.format = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", platformData.format);

				GUILayout.Space(2f);
				
				// CompressionQuality.

				var qualityTable = new int[]{ 0, 50, 100 };

				var qualityLabels = new string[]{ "Fast", "Normal", "Best" };
				var qualityIndex = qualityTable.IndexOf(x => x == platformData.compressionQuality); 

				EditorGUI.BeginChangeCheck();

				qualityIndex = EditorGUILayout.Popup("Compression Quality", qualityIndex, qualityLabels);

				if (EditorGUI.EndChangeCheck())
				{
					platformData.compressionQuality = qualityTable[qualityIndex];
				}

				GUILayout.Space(2f);
			}
		}

		private void DrawOptionSettingGUI()
		{
			EditorLayoutTools.ContentTitle("Options");

			using (new ContentsScope())
			{
				ignoreFolderView.DrawGUI();

				if (EditorLayoutTools.Header("Ignore FolderNames", $"{GetType().FullName}-IgnoreFolderNameView"))
				{
					using (new ContentsScope())
					{
						ignoreFolderNameView.DrawGUI();
					}
				}
			}
		}

		private void LoadIcons()
		{
			if (iconContents == null)
			{
				iconContents = new Dictionary<PlatformType, GUIContent>();

				iconContents.Add(PlatformType.Default, new GUIContent("Default"));
				iconContents.Add(PlatformType.Standalone, EditorGUIUtility.IconContent("BuildSettings.Standalone.Small"));
				iconContents.Add(PlatformType.Android, EditorGUIUtility.IconContent("BuildSettings.Android.Small"));
				iconContents.Add(PlatformType.iOS, EditorGUIUtility.IconContent("BuildSettings.iPhone.Small"));
			}
		}

		#region Parts of Texture Inspector.

		public void TextureShape()
		{
			textureData.textureShape = (TextureImporterShape)EditorGUILayout.EnumPopup("Texture Shape", textureData.textureShape);
		}

		public void sRGBTexture()
		{
			textureData.sRGBTexture = EditorGUILayout.Toggle("sRGB (Color Texture)", textureData.sRGBTexture);
		}

		public void AlphaSource()
		{
			textureData.alphaSource = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup("Alpha Source", textureData.alphaSource);
		}
		
		public void AlphaIsTransparency()
		{
			using (new DisableScope(textureData.alphaSource == TextureImporterAlphaSource.None))
			{
				textureData.alphaIsTransparency = EditorGUILayout.Toggle("Alpha Is Transparency", textureData.alphaIsTransparency);
			}
		}

		public void IgnorePngGamma()
		{
			textureData.ignorePngGamma = EditorGUILayout.Toggle("Ignore PNG Gamma", textureData.ignorePngGamma);
		}

		public void NpotScale()
		{
			textureData.npotScale = (TextureImporterNPOTScale)EditorGUILayout.EnumPopup("Non-Power of 2", textureData.npotScale);
		}

		public void IsReadable()
		{
			textureData.readable = EditorGUILayout.Toggle("Read/Write", textureData.readable);
		}

		public void ConvertToNormalMap()
		{
			textureData.convertToNormalMap = EditorGUILayout.Toggle("Create from GrayScale", textureData.convertToNormalMap);
		}

		public void HeightmapScale()
		{
			textureData.heightmapScale = EditorGUILayout.Slider("Bumpiness", textureData.heightmapScale, 0.0f, 0.3f);
		}

		public void NormalMapFilter()
		{
			textureData.normalMapFilter = (TextureImporterNormalFilter)EditorGUILayout.EnumPopup("Filtering", textureData.normalMapFilter);
		}

		public void StreamingMipMaps()
		{
			textureData.streamingMipmaps = EditorGUILayout.Toggle("Streaming Mip Maps", textureData.streamingMipmaps);
		}

		public void VirtualTextureOnly()
		{
			textureData.vtOnly = EditorGUILayout.Toggle("Virtual Texture Only", textureData.vtOnly);
		}

		public void GenerateMipMaps()
		{
			textureData.mipmapEnabled = EditorGUILayout.Toggle("Generate Mip Maps", textureData.mipmapEnabled);
		}

		public void BorderMipMaps()
		{
			textureData.borderMipmap = EditorGUILayout.Toggle("Border Mip Maps", textureData.borderMipmap);
		}

		public void MipMapFiltering()
		{
			textureData.mipmapFilter = (TextureImporterMipFilter)EditorGUILayout.EnumPopup("Mip Map Filtering", textureData.mipmapFilter);
		}

		public void MipMapsPreserveCoverage()
		{
			textureData.mipMapsPreserveCoverage = EditorGUILayout.Toggle("Mip Maps Preserve Coverage", textureData.mipMapsPreserveCoverage);
		}

		public void AlphaCutOffValue()
		{
			textureData.alphaTestReferenceValue = EditorGUILayout.FloatField("Alpha CutOff Value", textureData.alphaTestReferenceValue);
		}

		public void FadeOutMipMaps()
		{
			textureData.fadeOut = EditorGUILayout.Toggle("FadeOut Mip Maps", textureData.fadeOut);
		}

		public void FadeRange()
		{
			if (!textureData.fadeOut){ return; }
			
			EditorGUI.BeginChangeCheck();

			var start = (float)textureData.mipMapFadeDistanceStart;
			var end = (float)textureData.mipMapFadeDistanceEnd;

			EditorGUILayout.MinMaxSlider("Fade Range", ref start, ref end, 0, 10);

			if (EditorGUI.EndChangeCheck())
			{
				textureData.mipMapFadeDistanceStart = (int)start;
				textureData.mipMapFadeDistanceEnd = (int)end;
			}
		}

		public void SpriteMode()
		{
			textureData.spriteMode = (SpriteImportMode)EditorGUILayout.EnumPopup("Sprite Mode", textureData.spriteMode);
		}

		public void PixelsPerUnit()
		{
			textureData.spritePixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", textureData.spritePixelsPerUnit);
		}

		public void MeshType()
		{
			textureData.meshType = (SpriteMeshType)EditorGUILayout.EnumPopup("Mesh Type", textureData.meshType);
		}

		public void SpritePivot()
		{
			textureData.spriteAlignment = (SpriteAlignment)EditorGUILayout.EnumPopup("Sprite Pivot", textureData.spriteAlignment);
			
			if (textureData.spriteAlignment == SpriteAlignment.Custom)
			{
				using (new EditorGUI.IndentLevelScope(1))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.FlexibleSpace();

						EditorGUI.BeginChangeCheck();

						var spritePivot = new Vector2(textureData.spritePivotX, textureData.spritePivotY);

						spritePivot = EditorGUILayout.Vector2Field("", spritePivot);

						if (EditorGUI.EndChangeCheck())
						{
							textureData.spritePivotX = spritePivot.x;
							textureData.spritePivotY = spritePivot.y;
						}
					}
				}
			}
		}

		public void ExtrudeEdges()
		{
			textureData.spriteExtrude = (uint)EditorGUILayout.IntSlider("Extrude Edges", (int)textureData.spriteExtrude, 0, 32);
		}

		public void GeneratePhysicsShape()
		{
			textureData.spriteGenerateFallbackPhysicsShape = EditorGUILayout.Toggle("Generate Physics Shape", textureData.spriteGenerateFallbackPhysicsShape);
		}

		public void FilterMode()
		{
			textureData.filterMode = (FilterMode)EditorGUILayout.EnumPopup("FilterMode", textureData.filterMode);
		}

		public void WrapMode()
		{
			textureData.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("WrapMode", textureData.wrapMode);
		}

		public void AnisoLavel()
		{
			textureData.anisoLevel = EditorGUILayout.IntSlider("Aniso Lavel", textureData.anisoLevel, 0, 16);
		}

		#endregion

		public abstract void DrawTextureSettingGUI();
	}
}