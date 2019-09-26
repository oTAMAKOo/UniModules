
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextColorTag
{
    [CustomEditor(typeof(TextColorTag))]
    public sealed class TextColorTagInspector : Editor
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as TextColorTag;

            var setting = Reflection.GetPrivateField<TextColorTag, TextColorTagSetting>(instance, "setting");

            EditorGUI.BeginChangeCheck();

            var selection = EditorGUILayout.ObjectField("ColorTag File", setting, typeof(TextColorTagSetting), false) as TextColorTagSetting;

            if (EditorGUI.EndChangeCheck())
            {
                Reflection.SetPrivateField(instance, "setting", selection);
            }
        }
    }
}
