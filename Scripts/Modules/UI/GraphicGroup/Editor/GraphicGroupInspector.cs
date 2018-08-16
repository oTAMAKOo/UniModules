
using UnityEngine;
using UnityEditor;
using Extensions.Devkit;

namespace Modules.UI
{
    [CustomEditor(typeof(GraphicGroup))]
    public class GraphicGroupInspector : UnityEditor.Editor
    {
        private GraphicGroup instance = null;

        public override void OnInspectorGUI()
        {
            instance = target as GraphicGroup;

            DrawInspector();
        }

        private void DrawInspector()
        {
            GUILayout.Space(5f);

            EditorGUI.BeginChangeCheck();

            var raycastTarget = EditorGUILayout.Toggle("RaycastTarget", instance.raycastTarget);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("GraphicGroupInspector Undo", instance);
                instance.raycastTarget = raycastTarget;
            }

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();

            var colorTint = EditorGUILayout.ColorField("ColorTint", instance.ColorTint);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("GraphicGroupInspector Undo", instance);
                instance.ColorTint = colorTint;
            }
        }
    }
}