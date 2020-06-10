using UnityEditor;
using UnityEditor.UI;

namespace Modules.UI
{
    [CustomEditor(typeof(SnapScrollRect), true)]
    public sealed class SnapScrollRectInspector : ScrollRectEditor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fitRange"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
