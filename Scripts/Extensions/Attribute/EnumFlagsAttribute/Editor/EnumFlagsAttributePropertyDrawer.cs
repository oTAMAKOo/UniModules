
using UnityEngine;
using UnityEditor;
using System;

namespace Extensions
{
    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    public class EnumFlagsAttributePropertyDrawer : PropertyDrawer
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            prop.intValue = EditorGUI.MaskField(position, label, prop.intValue, prop.enumNames);
        }
    }
}