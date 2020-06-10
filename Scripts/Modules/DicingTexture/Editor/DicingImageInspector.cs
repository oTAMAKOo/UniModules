
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.Dicing
{
    [CustomEditor(typeof(DicingImage))]
    public sealed class DicingImageInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private string previewGuid = null;
        private Texture2D previewTexture = null;

        private static Texture2D previewBackdrop = null;

        private DicingImage instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as DicingImage;

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            var dicingTexture = EditorLayoutTools.ObjectField("DicingTexture", instance.DicingTexture, false);

            if(EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("DicingImageInspector-Undo", instance);
                instance.DicingTexture = dicingTexture;
            }

            // Color.
            EditorGUI.BeginChangeCheck();

            var color = EditorGUILayout.ColorField("Color", instance.Color, GUILayout.Height(18f));

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("DicingImageInspector-Undo", instance);
                instance.Color = color;
            }

            // Material.
            EditorGUI.BeginChangeCheck();

            var material = instance.Material.name == "Default UI Material" ? null : instance.Material;
            material = EditorLayoutTools.ObjectField("Material", material, false, GUILayout.Height(18f));

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("DicingImageInspector-Undo", instance);
                instance.Material = material;
            }

            // RaycastTarget.
            EditorGUI.BeginChangeCheck();

            var raycastTarget = EditorGUILayout.Toggle("RaycastTarget", instance.RaycastTarget);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("DicingImageInspector-Undo", instance);
                instance.RaycastTarget = raycastTarget;
            }

            // CrossFade.
            EditorGUI.BeginChangeCheck();

            var crossFade = EditorGUILayout.Toggle("CrossFade", instance.CrossFade, GUILayout.Height(18f));

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("DicingImageInspector-Undo", instance);
                instance.CrossFade = crossFade;
            }

            if(instance.CrossFade)
            {
                EditorGUI.BeginChangeCheck();

                var crossFadeTime = EditorGUILayout.FloatField("FadeTime", instance.CrossFadeTime, GUILayout.Height(18f));

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("DicingImageInspector-Undo", instance);
                    instance.CrossFadeTime = crossFadeTime;
                }
            }

            GUILayout.Space(2f);

            if (instance.DicingTexture != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                    {
                        Action<string> onSelection = x =>
                        {
                            instance.PatternName = x;

                            EditorUtility.SetDirty(instance);
                            InternalEditorUtility.RepaintAllViews();
                        };

                        var selection = instance.Current != null ? instance.Current.textureName : null;

                        DicingSpriteSelector.Show(instance.DicingTexture, selection, onSelection, null);
                    }

                    if (instance.Current != null)
                    {
                        EditorGUILayout.SelectableLabel(instance.Current.textureName, new GUIStyle("TextArea"), GUILayout.Height(18f));
                    }
                }

                GUILayout.Space(5f);

                if (instance.Current != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(EditorGUIUtility.labelWidth);

                        if (GUILayout.Button("SetNativeSize"))
                        {
                            instance.SetNativeSize();
                        }
                    }                    
                }
            }
        }
        
        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if(instance.Current == null) { return; }

            if(previewTexture == null || previewGuid != instance.Current.guid)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(instance.Current.guid);
                previewTexture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture2D;

                previewGuid = instance.Current.guid;
            }

            if(previewTexture == null) { return; }

            if(previewBackdrop == null)
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
