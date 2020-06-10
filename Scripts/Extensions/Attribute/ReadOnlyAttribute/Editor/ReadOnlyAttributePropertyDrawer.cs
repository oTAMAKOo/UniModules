﻿﻿
using UnityEngine;
using UnityEditor;

namespace Extensions
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public sealed class ReadOnlyAttributePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
 
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(true);
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
