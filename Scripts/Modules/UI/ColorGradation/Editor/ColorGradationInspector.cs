
using UnityEngine;
using System;
using UnityEditor;
using Extensions.Devkit;

namespace Modules.UI
{
    [CustomEditor(typeof(ColorGradation))]
    public sealed class ColorGradationInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private ColorGradation instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as ColorGradation;

            serializedObject.Update();

            DrawInspector();
        }

        private void DrawInspector()
        {
            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();

            var mode = (ColorGradation.ModeType)EditorGUILayout.EnumPopup("Mode", instance.Mode);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                instance.Mode = mode;
            }

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();

            var direction = (ColorGradation.DirectionType)EditorGUILayout.EnumPopup("Direction", instance.Direction);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                instance.Direction = direction;
            }

            GUILayout.Space(2f);

            switch (instance.Mode)
            {
                case ColorGradation.ModeType.Simple:
                    DrawSimpleColors();
                    break;

                case ColorGradation.ModeType.Gradient:
                    DrawGradientFields();
                    break;
            }
        }

        private void DrawSimpleColors()
        {
            switch (instance.Direction)
            {
                case ColorGradation.DirectionType.Vertical:
                    DrawColorField("ColorTop", instance.ColorTop, x => instance.ColorTop = x);
                    DrawColorField("ColorBottom", instance.ColorBottom, x => instance.ColorBottom = x);
                    break;

                case ColorGradation.DirectionType.Horizontal:
                    DrawColorField("ColorLeft", instance.ColorLeft, x => instance.ColorLeft = x);
                    DrawColorField("ColorRight", instance.ColorRight, x => instance.ColorRight = x);
                    break;

                case ColorGradation.DirectionType.Both:
                    DrawColorField("ColorTop", instance.ColorTop, x => instance.ColorTop = x);
                    DrawColorField("ColorBottom", instance.ColorBottom, x => instance.ColorBottom = x);
                    DrawColorField("ColorLeft", instance.ColorLeft, x => instance.ColorLeft = x);
                    DrawColorField("ColorRight", instance.ColorRight, x => instance.ColorRight = x);
                    break;
            }
        }

        private void DrawGradientFields()
        {
            switch (instance.Direction)
            {
                case ColorGradation.DirectionType.Vertical:
                    DrawGradientField("VerticalGradient", "verticalGradient");
                    break;

                case ColorGradation.DirectionType.Horizontal:
                    DrawGradientField("HorizontalGradient", "horizontalGradient");
                    break;

                case ColorGradation.DirectionType.Both:
                    DrawGradientField("VerticalGradient", "verticalGradient");
                    DrawGradientField("HorizontalGradient", "horizontalGradient");
                    break;
            }
        }

        private void DrawColorField(string label, Color current, Action<Color> setter)
        {
            EditorGUI.BeginChangeCheck();

            var color = EditorGUILayout.ColorField(label, current);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                setter(color);
            }
        }

        private void DrawGradientField(string label, string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);

            if (property == null){ return; }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(property, new GUIContent(label));

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                serializedObject.ApplyModifiedProperties();

                instance.Refresh();
            }
        }
    }
}
