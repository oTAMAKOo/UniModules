
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using UnityEditor.Callbacks;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextOutline))]
    public class TextOutlineInspector : TextEffectBaseInspector
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            var instance = target as TextOutline;

            var color = Reflection.GetPrivateField<TextOutline, Color>(instance, "color");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Color", color);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "color", color);
            }

            var distance = Reflection.GetPrivateField<TextOutline, float>(instance, "distance");

            EditorGUI.BeginChangeCheck();

            distance = EditorGUILayout.DelayedFloatField("Distance", distance);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue(instance, "distance", distance);
            }
        }

        private void SetValue<TValue>(TextOutline instance, string fieldName, TValue value)
        {
            UnityEditorUtility.RegisterUndo("TextOutlineInspector Undo", instance);
            Reflection.SetPrivateField(instance, fieldName, value);

            update = true;
        }
    }
}
