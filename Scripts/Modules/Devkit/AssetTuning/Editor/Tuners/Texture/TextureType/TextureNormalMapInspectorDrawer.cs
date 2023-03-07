
using UnityEngine;
using UnityEditor;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
    public sealed class TextureNormalMapInspectorDrawer :  TextureDataInspectorDrawer
    {
		private bool foldoutAdvanced = false;

		public TextureNormalMapInspectorDrawer(TextureData textureData) : base(textureData){}
		
		public override void DrawTextureSettingGUI()
		{
			TextureShape();

			GUILayout.Space(2f);

			IgnorePngGamma();

			ConvertToNormalMap();

			if (textureData.convertToNormalMap)
			{
				using (new EditorGUI.IndentLevelScope(1))
				{
					HeightmapScale();

					NormalMapFilter();
				}
			}
			
			GUILayout.Space(2f);

			using (new EditorGUI.IndentLevelScope(1))
			{
				foldoutAdvanced = EditorGUILayout.Foldout(foldoutAdvanced, "Advanced");
			
				if (foldoutAdvanced)
				{
					NpotScale();

					IsReadable();

					StreamingMipMaps();

					VirtualTextureOnly();

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

			AnisoLavel();
		}
    }
}