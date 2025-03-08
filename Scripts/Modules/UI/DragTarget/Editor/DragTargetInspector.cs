
using UnityEngine;
using UnityEditor;

namespace Modules.UI
{
    [CustomEditor(typeof(DragObject), true)]
    public sealed class DragTargetInspector : Editor
    {
        //----- params -----

        //----- field -----

        private SerializedProperty targetProperty = null;
        private SerializedProperty horizontalProperty = null;
        private SerializedProperty verticalProperty = null;
        private SerializedProperty inertiaProperty = null;
        private SerializedProperty dampeningRateProperty = null;
        private SerializedProperty inertiaRoundingProperty = null;
        private SerializedProperty constrainWithinCanvasProperty = null;
        private SerializedProperty constrainDragProperty = null;
        private SerializedProperty constrainInertiaProperty = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            targetProperty = serializedObject.FindProperty("target");
            horizontalProperty = serializedObject.FindProperty("horizontal");
            verticalProperty = serializedObject.FindProperty("vertical");
            inertiaProperty = serializedObject.FindProperty("inertia");
            dampeningRateProperty = serializedObject.FindProperty("dampeningRate");
            inertiaRoundingProperty = serializedObject.FindProperty("inertiaRounding");
            constrainWithinCanvasProperty = serializedObject.FindProperty("constrainWithinCanvas");
            constrainDragProperty = serializedObject.FindProperty("constrainDrag");
            constrainInertiaProperty = serializedObject.FindProperty("constrainInertia");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(targetProperty);
            EditorGUILayout.PropertyField(horizontalProperty);
            EditorGUILayout.PropertyField(verticalProperty);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Inertia", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(inertiaProperty, new GUIContent("Enable"));

                if (inertiaProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(dampeningRateProperty);
                    EditorGUILayout.PropertyField(inertiaRoundingProperty);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Constrain Within Canvas", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(constrainWithinCanvasProperty, new GUIContent("Enable"));

                if (constrainWithinCanvasProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(constrainDragProperty, new GUIContent("Constrain Drag"));
                    EditorGUILayout.PropertyField(constrainInertiaProperty, new GUIContent("Constrain Inertia"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}