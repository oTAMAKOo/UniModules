
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextShadow))]
    public class TextShadowInspector : TextEffectBaseInspector
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

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Color", color);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "color", color);
            }

            var offsetX = Reflection.GetPrivateField<TextShadow, float>(instance, "offsetX");

            EditorGUI.BeginChangeCheck();

            offsetX = EditorGUILayout.DelayedFloatField("OffsetX", offsetX);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "offsetX", offsetX);
            }

            var offsetY = Reflection.GetPrivateField<TextShadow, float>(instance, "offsetY");

            EditorGUI.BeginChangeCheck();

            offsetY = EditorGUILayout.DelayedFloatField("offsetY", offsetY);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "offsetY", offsetY);
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
                                SetValue(instance, "color", textShadow.Color);
                                SetValue(instance, "offsetX", textShadow.Offset.x);
                                SetValue(instance, "offsetY", textShadow.Offset.y);
                            }
                        }

                        GUILayout.Space(5f);
                    }
                }
            }
        }

        private void SetValue<TValue>(TextShadow instance, string fieldName, TValue value)
        {
            UnityEditorUtility.RegisterUndo("TextShadowInspector Undo", instance);
            Reflection.SetPrivateField(instance, fieldName, value);

            update = true;
        }
    }
}
