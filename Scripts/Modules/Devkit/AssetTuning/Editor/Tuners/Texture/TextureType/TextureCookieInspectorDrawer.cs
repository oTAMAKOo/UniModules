
using UnityEngine;
using UnityEditor;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
    public sealed class TextureCookieInspectorDrawer : TextureDataInspectorDrawer
    {
        private bool foldoutAdvanced = false;

        public TextureCookieInspectorDrawer(TextureData textureData) : base(textureData){}
		
        public override void DrawTextureSettingGUI()
        {
            AlphaSource();

            AlphaIsTransparency();

            IgnorePngGamma();

            GUILayout.Space(2f);

            using (new EditorGUI.IndentLevelScope(1))
            {
                foldoutAdvanced = EditorGUILayout.Foldout(foldoutAdvanced, "Advanced");
			
                if (foldoutAdvanced)
                {
                    NpotScale();

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