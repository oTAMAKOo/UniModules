
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextShadow))]
    public class TextShadowInspector : TextEffectBaseInspector
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as TextShadow;

            var color = Reflection.GetPrivateField<TextShadow, Color>(instance, "color");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Color", color);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "color", color);
            }

            var offsetX = Reflection.GetPrivateField<TextShadow, float>(instance, "offsetX");

            EditorGUI.BeginChangeCheck();

            offsetX = EditorGUILayout.DelayedFloatField("OffsetX", offsetX);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "offsetX", offsetX);
            }

            var offsetY = Reflection.GetPrivateField<TextShadow, float>(instance, "offsetY");

            EditorGUI.BeginChangeCheck();

            offsetY = EditorGUILayout.DelayedFloatField("offsetY", offsetY);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "offsetY", offsetY);
            }
        }

        private void SetValue<TValue>(TextShadow instance, string fieldName, TValue value)
        {
            UnityEditorUtility.RegisterUndo("TextShadowInspector Undo", instance);
            Reflection.SetPrivateField(instance, fieldName, value);

            update = true;
        }
    }
}
