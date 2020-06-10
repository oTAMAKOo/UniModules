﻿
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextOutline))]
    public sealed class TextOutlineInspector : TextEffectBaseInspector
    {
        //----- params -----

        //----- field -----

        private TextOutline instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as TextOutline;

            var color = Reflection.GetPrivateField<TextOutline, Color>(instance, "color");
            var distance = Reflection.GetPrivateField<TextOutline, float>(instance, "distance");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Color", color);
            distance = EditorGUILayout.DelayedFloatField("Distance", distance);

            if (EditorGUI.EndChangeCheck())
            {
                UpdateParams(instance, color, distance);
                return;
            }

            DrawMaterialSelector(instance);
        }

        protected override void DrawSelectorContents(TextEffectBase[] targets)
        {
            foreach (var target in targets)
            {
                var textOutline = target as TextOutline;

                if (textOutline == null) { continue; }

                using (new ContentsScope())
                {
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Height(18f)))
                    {
                        GUILayout.Space(5f);

                        using (new DisableScope(true))
                        {
                            EditorGUILayout.ColorField(textOutline.Color, GUILayout.Width(50f));
                        }

                        GUILayout.Space(2f);

                        var colorText = ColorUtility.ToHtmlStringRGBA(textOutline.Color);
                        EditorGUILayout.SelectableLabel(colorText, new GUIStyle("TextArea"), GUILayout.Width(95f), GUILayout.Height(18f));

                        GUILayout.Space(2f);

                        EditorGUILayout.FloatField(textOutline.Distance, GUILayout.Width(50f), GUILayout.Height(18f));

                        GUILayout.FlexibleSpace();

                        if(instance.Color != textOutline.Color || instance.Distance != textOutline.Distance)
                        {
                            if (GUILayout.Button("Apply", GUILayout.Width(65f)))
                            {
                                UpdateParams(instance, textOutline.Color, textOutline.Distance);
                            }
                        }

                        GUILayout.Space(5f);
                    }
                }
            }
        }

        private void UpdateParams(TextOutline instance, Color color, float distance)
        {
            UnityEditorUtility.RegisterUndo("TextOutlineInspector Undo", instance);

            Reflection.SetPrivateField(instance, "color", color);
            Reflection.SetPrivateField(instance, "distance", distance);

            Apply();
        }
    }
}
