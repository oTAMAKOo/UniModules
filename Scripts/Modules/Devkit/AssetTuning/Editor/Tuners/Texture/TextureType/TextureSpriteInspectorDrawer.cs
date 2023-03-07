
using Extensions.Devkit;
using UnityEngine;
using UnityEditor;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
    public sealed class TextureSpriteInspectorDrawer : TextureDataInspectorDrawer
    {
		private bool foldoutAdvanced = false;

		public TextureSpriteInspectorDrawer(TextureData textureData) : base(textureData) { }
		
		public override void DrawTextureSettingGUI()
		{
			GUILayout.Space(2f);

			SpriteMode();

			using (new EditorGUI.IndentLevelScope(1))
			{
				PixelsPerUnit();

				MeshType();

				ExtrudeEdges();

				SpritePivot();

				GeneratePhysicsShape();
			}
			
			GUILayout.Space(2f);

			using (new EditorGUI.IndentLevelScope(1))
			{
				foldoutAdvanced = EditorGUILayout.Foldout(foldoutAdvanced, "Advanced");
			
				if (foldoutAdvanced)
				{
					sRGBTexture();

					AlphaSource();

					AlphaIsTransparency();

					IgnorePngGamma();

					IsReadable();

					GenerateMipMaps();

					if (textureData.mipmapEnabled)
					{
						using (new EditorGUI.IndentLevelScope(1))
						{
							BorderMipMaps();

							MipMapFiltering();

							MipMapsPreserveCoverage();
							
							if (textureData.mipMapsPreserveCoverage)
							{
								using (new EditorGUI.IndentLevelScope(1))
								{
									AlphaCutOffValue();
								}
							}
						
							FadeOutMipMaps();

							FadeRange();
						}
					}
				}
			}

			GUILayout.Space(2f);
			
			WrapMode();

			FilterMode();

			using (new DisableScope(true))
			{
				AnisoLavel();
			}
		}
	}
}