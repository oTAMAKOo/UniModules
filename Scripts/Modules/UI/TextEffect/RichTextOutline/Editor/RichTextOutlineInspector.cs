
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(RichTextOutline))]
    public sealed class RichTextOutlineInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private RichTextOutline instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as RichTextOutline;

            DrawInspector();
        }

        private void DrawInspector()
        {
            var copyCount = Reflection.GetPrivateField<RichTextOutline, int>(instance, "copyCount");

            EditorGUI.BeginChangeCheck();

            copyCount = EditorGUILayout.IntField("CopyCount", copyCount);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("copyCount", copyCount);
            }

            var color = Reflection.GetPrivateField<RichTextOutline, Color>(instance, "color");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Effect Color", color);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("color", color);
            }

            var distance = Reflection.GetPrivateField<RichTextOutline, Vector2>(instance, "distance");

            EditorGUI.BeginChangeCheck();

            distance = EditorGUILayout.Vector2Field("Effect Distance", distance);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("distance", distance);
            }

            var useGraphicAlpha = Reflection.GetPrivateField<RichTextOutline, bool>(instance, "useGraphicAlpha");

            EditorGUI.BeginChangeCheck();

            useGraphicAlpha = EditorGUILayout.Toggle("Use Graphic Alpha", useGraphicAlpha);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("useGraphicAlpha", useGraphicAlpha);
            }
        }

        private void SetValue<TValue>(string fieldName, TValue value)
        {
            UnityEditorUtility.RegisterUndo("RichTextOutlineInspector Undo", instance);
            Reflection.SetPrivateField(instance, fieldName, value);
            Reflection.InvokePrivateMethod(instance, "OnValidate");
        }
    }
}
