﻿﻿
using UnityEngine;
using UnityEditor;

namespace Extensions.Serialize
{
    public abstract class SerializableNullablePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var hasValueRect = new Rect(position.x, position.y, 30, position.height);
            var valueRect = new Rect(position.x + 25, position.y, 80, position.height);

            EditorGUI.PropertyField(hasValueRect, property.FindPropertyRelative("hasValue"), GUIContent.none);
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("value"), GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}