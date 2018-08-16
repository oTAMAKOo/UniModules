﻿﻿
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions.Devkit;

namespace Extensions.Devkit
{
    [CustomPropertyDrawer(typeof(Prefab))]
    public class PrefabPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) * 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var prefabProperty = property.FindPropertyRelative("prefab");
            var parentProperty = property.FindPropertyRelative("parent");

            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var px = position.x;
            var py = position.y;

            var originLabelWidth = EditorLayoutTools.SetLabelWidth(50f);

            //====== Prefab ======

            EditorGUI.BeginChangeCheck();

            var prefabRect = new Rect(px, py, position.width, position.height * 0.5f);

            var prefab = (GameObject)EditorGUI.ObjectField(prefabRect, "Prefab", prefabProperty.objectReferenceValue, typeof(GameObject), false);
    
            if (EditorGUI.EndChangeCheck())
            {
                prefabProperty.objectReferenceValue = prefab;
                property.serializedObject.ApplyModifiedProperties();
            }

            //====== Parent ======

            EditorGUI.BeginChangeCheck();

            var parentRect = new Rect(px, py + position.height * 0.5f, position.width, position.height * 0.5f);

            var parent = (GameObject)EditorGUI.ObjectField(parentRect, "Parent", parentProperty.objectReferenceValue, typeof(GameObject), true);

            if (EditorGUI.EndChangeCheck())
            {
                var gameObjects = parent != null ? UnityEditorUtility.FindAllObjectsInHierarchy() : null;

                if (parent == null || gameObjects.Contains(parent))
                {
                    parentProperty.objectReferenceValue = parent;
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError("ParentはHierarchy内のGameObjectである必要があります");
                }
            }

            EditorGUI.indentLevel = indent;

            EditorLayoutTools.SetLabelWidth(originLabelWidth);

            EditorGUI.EndProperty();
        }
    }
}