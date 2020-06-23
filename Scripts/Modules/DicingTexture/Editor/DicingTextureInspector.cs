
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.UI.Extension;

namespace Modules.Dicing
{
    [CustomEditor(typeof(DicingTexture))]
    public sealed class DicingTextureInspector : Editor
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
            var dicingTexture = target as DicingTexture;

            EditorLayoutTools.DrawContentTitle("Contents");

            using (new ContentsScope())
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(350f)))
                {
                    var sourceData = dicingTexture.GetAllDicingSource();

                    for (var i = 0; i < sourceData.Length; i++)
                    {
                        var data = sourceData[i];

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new LabelWidthScope(20f))
                            {
                                EditorGUILayout.PrefixLabel(i.ToString(), EditorStyles.miniLabel);
                            }

                            EditorGUILayout.SelectableLabel(data.textureName, new GUIStyle("TextArea"), GUILayout.Height(18f));

                            using (new DisableScope(previewGuid == data.guid))
                            {
                                using (new EditorGUILayout.VerticalScope(GUILayout.Width(50f)))
                                {
                                    GUILayout.Space(3f);

                                    if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(50f)))
                                    {
                                        previewGuid = data.guid;

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
