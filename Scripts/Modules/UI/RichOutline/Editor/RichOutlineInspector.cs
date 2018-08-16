﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI
{
    [CustomEditor(typeof(RichOutline))]
    public class RichOutlineInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private RichOutline instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as RichOutline;

            DrawInspector();
        }

        private void DrawInspector()
        {
            var copyCount = Reflection.GetPrivateField<RichOutline, int>(instance, "copyCount");

            EditorGUI.BeginChangeCheck();

            copyCount = EditorGUILayout.IntField("CopyCount", copyCount);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("copyCount", copyCount);
            }

            var color = Reflection.GetPrivateField<RichOutline, Color>(instance, "color");

            EditorGUI.BeginChangeCheck();

            color = EditorGUILayout.ColorField("Effect Color", color);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("color", color);
            }

            var distance = Reflection.GetPrivateField<RichOutline, Vector2>(instance, "distance");

            EditorGUI.BeginChangeCheck();

            distance = EditorGUILayout.Vector2Field("Effect Distance", distance);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("distance", distance);
            }

            var useGraphicAlpha = Reflection.GetPrivateField<RichOutline, bool>(instance, "useGraphicAlpha");

            EditorGUI.BeginChangeCheck();

            useGraphicAlpha = EditorGUILayout.Toggle("Use Graphic Alpha", useGraphicAlpha);

            if (EditorGUI.EndChangeCheck())
            {
                SetValue("useGraphicAlpha", useGraphicAlpha);
            }
        }

        private void SetValue<TValue>(string fieldName, TValue value)
        {
            UnityEditorUtility.RegisterUndo("RichOutlineInspector Undo", instance);
            Reflection.SetPrivateField(instance, fieldName, value);
            Reflection.InvokePrivateMethod(instance, "OnValidate");
        }
    }
}