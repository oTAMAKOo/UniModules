
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextShadow))]
    public sealed class TextShadowInspector : TextEffectBaseInspector
    {
        //----- params -----

        //----- field -----

        private TextShadow instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as TextShadow;

            var color = Reflection.GetPrivateField<TextShadow, Color>(instance, "color");
            var offsetX = Reflection.GetPrivateField<TextShadow, float>(instance, "offsetX");
            var offsetY = Reflection.GetPrivateField<TextShadow, float>(instance, "offsetY");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Color", color);

            offsetX = EditorGUILayout.DelayedFloatField("OffsetX", offsetX);
            
            offsetY = EditorGUILayout.DelayedFloatField("OffsetY", offsetY);

            if (EditorGUI.EndChangeCheck())
            {
                UpdateParams(instance, color, offsetX, offsetY);
                return;
            }

            DrawMaterialSelector(instance);
        }

        protected override void DrawSelectorContents(TextEffectBase[] targets)
        {
            foreach (var target in targets)
            {
                var textShadow = target as TextShadow;

                if (textShadow == null) { continue; }

                using (new ContentsScope())
                {
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(18f)))
                    {
                        GUILayout.Space(5f);

                        using (new DisableScope(true))
                        {
                            EditorGUILayout.ColorField(textShadow.Color, GUILayout.Width(50f));
                        }

                        GUILayout.Space(2f);

                        var colorText = ColorUtility.ToHtmlStringRGBA(textShadow.Color);
                        EditorGUILayout.SelectableLabel(colorText, new GUIStyle("TextArea"), GUILayout.Width(95f), GUILayout.Height(18f));

                        GUILayout.Space(2f);

                        EditorGUILayout.FloatField(textShadow.Offset.x, GUILayout.Width(50f), GUILayout.Height(18f));

                        GUILayout.Space(2f);

                        EditorGUILayout.FloatField(textShadow.Offset.y, GUILayout.Width(50f), GUILayout.Height(18f));

                        GUILayout.FlexibleSpace();

                        if (instance.Color != textShadow.Color || instance.Offset != textShadow.Offset)
                        {
                            if (GUILayout.Button("Apply", GUILayout.Width(65f)))
                            {
                                UpdateParams(instance, textShadow.Color, textShadow.Offset.x, textShadow.Offset.y);
                            }
                        }

                        GUILayout.Space(5f);
                    }
                }
            }
        }

        private void UpdateParams(TextShadow instance, Color color, float offsetX, float offsetY)
        {
            UnityEditorUtility.RegisterUndo("TextShadowInspector Undo", instance);

            Reflection.SetPrivateField(instance, "color", color);
            Reflection.SetPrivateField(instance, "offsetX", offsetX);
            Reflection.SetPrivateField(instance, "offsetY", offsetY);

            Apply();
        }
    }
}
