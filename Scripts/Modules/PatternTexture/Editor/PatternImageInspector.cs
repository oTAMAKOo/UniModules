
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.PatternTexture
{
    [CustomEditor(typeof(PatternImage))]
    public sealed class PatternImageInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private string previewGuid = null;
        private Texture2D previewTexture = null;

        private static Texture2D previewBackdrop = null;

        private PatternImage instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as PatternImage;

            EditorGUI.BeginChangeCheck();

            var patternTexture = EditorLayoutTools.ObjectField("PatternTexture", instance.PatternTexture, false);

            if(EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("PatternImageInspector-Undo", instance);
                instance.PatternTexture = patternTexture;
            }

            if (instance.PatternTexture != null)
            {
                // Color.
                EditorGUI.BeginChangeCheck();

                var color = EditorGUILayout.ColorField("Color", instance.Color, GUILayout.Height(18f));

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("PatternImageInspector-Undo", instance);
                    instance.Color = color;
                }

                // Material.
                EditorGUI.BeginChangeCheck();

                var material = instance.Material.name == "Default UI Material" ? null : instance.Material;
                material = EditorLayoutTools.ObjectField("Material", material, false, GUILayout.Height(18f));

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("PatternImageInspector-Undo", instance);
                    instance.Material = material;
                }

                // RaycastTarget.

                var hasAlphaMap = instance.PatternTexture.HasAlphaMap;

                using (new DisableScope(!hasAlphaMap))
                {
                    EditorGUI.BeginChangeCheck();

                    var raycastTarget = EditorGUILayout.Toggle("RaycastTarget", instance.RaycastTarget);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("PatternImageInspector-Undo", instance);
                        instance.RaycastTarget = raycastTarget;
                    }
                }

                if (!hasAlphaMap)
                {
                    EditorGUILayout.HelpBox("Require generate alpha map for RaycastTarget", MessageType.Info);
                }

                // CrossFade.
                EditorGUI.BeginChangeCheck();

                var crossFade = EditorGUILayout.Toggle("CrossFade", instance.CrossFade, GUILayout.Height(18f));

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("PatternImageInspector-Undo", instance);
                    instance.CrossFade = crossFade;
                }

                if (instance.CrossFade)
                {
                    EditorGUI.BeginChangeCheck();

                    var crossFadeTime = EditorGUILayout.FloatField("FadeTime", instance.CrossFadeTime, GUILayout.Height(18f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("PatternImageInspector-Undo", instance);
                        instance.CrossFadeTime = crossFadeTime;
                    }
                }

                GUILayout.Space(2f);

                if (instance.PatternTexture != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(76f)))
                        {
                            GUILayout.Space(2f);

                            if (EditorLayoutTools.PrefixButton("Sprite", GUILayout.Width(76f), GUILayout.Height(18f)))
                            {
                                Action<string> onSelection = x =>
                                {
                                    instance.PatternName = x;

                                    EditorUtility.SetDirty(instance);
                                    InternalEditorUtility.RepaintAllViews();
                                };

                                var selection = instance.Current != null ? instance.Current.TextureName : null;

                                PatternSpriteSelector.Show(instance.PatternTexture, selection, onSelection, null);
                            }
                        }

                        GUILayout.Space(4f);

                        if (instance.Current != null)
                        {
                            EditorGUILayout.SelectableLabel(instance.Current.TextureName, EditorStyles.textArea, GUILayout.Height(18f));
                        }
                    }

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
        }
        
        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if(instance == null || instance.Current == null) { return; }

            var current = instance.Current;

            if (previewTexture == null || previewGuid != current.Guid)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(current.Guid);
                previewTexture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture2D;

                previewGuid = current.Guid;
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
