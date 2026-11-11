using UnityEngine;
using UnityEditor;

namespace Modules.UI
{
    [CanEditMultipleObjects, CustomEditor(typeof(SlicedFillGraphic), true)]
    public sealed class SlicedFillGraphicInspector : Editor
    {
        //----- params -----

        //----- field -----

        private SlicedFillGraphic instance = null;

        private SerializedProperty fillOriginProperty = null;
        private SerializedProperty fillAmountProperty = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as SlicedFillGraphic;

            fillOriginProperty = serializedObject.FindProperty("fillOrigin");
            fillAmountProperty = serializedObject.FindProperty("fillAmount");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(fillOriginProperty, new GUIContent("Fill Origin"));
            EditorGUILayout.PropertyField(fillAmountProperty, new GUIContent("Fill Amount"));

            var changed = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (changed && instance != null)
            {
                instance.Refresh();
            }

            EditorGUILayout.Separator();

            SlicedFillGraphicWarning.Draw(instance);
        }
    }
}
