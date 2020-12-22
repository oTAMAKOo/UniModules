
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.PatternTexture
{
    [CustomEditor(typeof(PatternTexture))]
    public sealed class PatternTextureInspector : Editor
    {
        //----- params -----

        //----- field -----

        private string previewGuid = null;

        private Texture previewTexture = null;

        private Vector2 scrollPosition = Vector2.zero;

        private static Texture2D previewBackdrop = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var patternTexture = target as PatternTexture;

            EditorLayoutTools.ContentTitle("Contents");

            using (new ContentsScope())
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(350f)))
                {
                    var sourceData = patternTexture.GetAllPatternData();

                    for (var i = 0; i < sourceData.Count; i++)
                    {
                        var data = sourceData[i];

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new LabelWidthScope(20f))
                            {
                                EditorGUILayout.PrefixLabel(i.ToString(), EditorStyles.miniLabel);
                            }
                            
                            EditorGUILayout.SelectableLabel(data.TextureName, EditorStyles.textArea, GUILayout.Height(18f));

                            using (new DisableScope(previewGuid == data.Guid))
                            {
                                using (new EditorGUILayout.VerticalScope(GUILayout.Width(50f)))
                                {
                                    GUILayout.Space(3f);

                                    if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(50f)))
                                    {
                                        previewGuid = data.Guid;

                                        var assetPath = AssetDatabase.GUIDToAssetPath(previewGuid);

                                        previewTexture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture;

                                        Repaint();
                                    }
                                }
                            }
                        }
                    }
                    
                    scrollPosition = scrollViewScope.scrollPosition;
                }
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (previewTexture == null) { return; }

            if (previewBackdrop == null)
            {
                var c1 = new Color(1f, 1f, 1f, 0.8f);
                var c2 = new Color(1f, 1f, 1f, 1f);

                previewBackdrop = TextureUtility.CreateCheckerTex(c1, c2, 32);
            }

            var uv = new Rect(0f, 0f, 1f, 1f);

            var scaleX = r.width / uv.width;
            var scaleY = r.height / uv.height;

            var aspect = (scaleY / scaleX) / ((float)previewTexture.height / previewTexture.width);
            var clipRect = r;

            if (aspect != 1f)
            {
                if (aspect < 1f)
                {
                    var padding = r.width * (1f - aspect) * 0.5f;
                    clipRect.xMin += padding;
                    clipRect.xMax -= padding;
                }
                else
                {
                    var padding = r.height * (1f - 1f / aspect) * 0.5f;
                    clipRect.yMin += padding;
                    clipRect.yMax -= padding;
                }
            }

            EditorLayoutTools.DrawTiledTexture(clipRect, previewBackdrop);

            GUI.DrawTextureWithTexCoords(clipRect, previewTexture, uv);
        }
    }
}
