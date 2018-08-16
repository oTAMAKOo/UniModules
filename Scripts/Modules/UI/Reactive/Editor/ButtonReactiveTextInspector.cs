﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Reactive
{
    [CustomEditor(typeof(ButtonReactiveText))]
    public class ButtonReactiveTextInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private ButtonReactiveText instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as ButtonReactiveText;

            DrawDefaultInspector();

            EditorGUI.BeginChangeCheck();

            var useShadow = Reflection.GetPrivateField<ButtonReactiveText, bool>(instance, "useShadow");

            useShadow = EditorGUILayout.Toggle("Shadow", useShadow);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                Reflection.SetPrivateField(instance, "useShadow", useShadow);
            }

            EditorGUI.BeginChangeCheck();

            var useOutline = Reflection.GetPrivateField<ButtonReactiveText, bool>(instance, "useOutline");

            useOutline = EditorGUILayout.Toggle("Outline", useOutline);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                Reflection.SetPrivateField(instance, "useOutline", useOutline);
            }

            var backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            var labelColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

            var originLabelWidth = EditorLayoutTools.SetLabelWidth(60f);

            EditorLayoutTools.DrawLabelWithBackground("Text", backgroundColor, labelColor);

            using (new EditorGUILayout.HorizontalScope())
            {
                // enableColor.

                EditorGUI.BeginChangeCheck();

                var enableColor = Reflection.GetPrivateField<ButtonReactiveText, Color>(instance, "enableColor");

                enableColor = EditorGUILayout.ColorField("Enable", enableColor);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                    Reflection.SetPrivateField(instance, "enableColor", enableColor);
                }

                // disableColor.

                EditorGUI.BeginChangeCheck();

                var disableColor = Reflection.GetPrivateField<ButtonReactiveText, Color>(instance, "disableColor");

                disableColor = EditorGUILayout.ColorField("Disable", disableColor);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                    Reflection.SetPrivateField(instance, "disableColor", disableColor);
                }
            }

            if (useShadow)
            {
                EditorLayoutTools.DrawLabelWithBackground("Shadow", backgroundColor, labelColor);

                using (new EditorGUILayout.HorizontalScope())
                {
                    // enableColor.

                    EditorGUI.BeginChangeCheck();

                    var enableShadowColor = Reflection.GetPrivateField<ButtonReactiveText, Color>(instance, "enableShadowColor");

                    enableShadowColor = EditorGUILayout.ColorField("Enable", enableShadowColor);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                        Reflection.SetPrivateField(instance, "enableShadowColor", enableShadowColor);
                    }

                    // disableColor.

                    EditorGUI.BeginChangeCheck();

                    var disableShadowColor = Reflection.GetPrivateField<ButtonReactiveText, Color>(instance, "disableShadowColor");

                    disableShadowColor = EditorGUILayout.ColorField("Disable", disableShadowColor);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                        Reflection.SetPrivateField(instance, "disableShadowColor", disableShadowColor);
                    }
                }
            }

            if (useOutline)
            {
                EditorLayoutTools.DrawLabelWithBackground("Outline", backgroundColor, labelColor);

                using (new EditorGUILayout.HorizontalScope())
                {
                    // enableColor.

                    EditorGUI.BeginChangeCheck();

                    var enableOutlineColor = Reflection.GetPrivateField<ButtonReactiveText, Color>(instance, "enableOutlineColor");

                    enableOutlineColor = EditorGUILayout.ColorField("Enable", enableOutlineColor);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                        Reflection.SetPrivateField(instance, "enableOutlineColor", enableOutlineColor);
                    }

                    // disableColor.

                    EditorGUI.BeginChangeCheck();

                    var disableOutlineColor = Reflection.GetPrivateField<ButtonReactiveText, Color>(instance, "disableOutlineColor");

                    disableOutlineColor = EditorGUILayout.ColorField("Disable", disableOutlineColor);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("ButtonReactiveTextInspector Undo", instance);
                        Reflection.SetPrivateField(instance, "disableOutlineColor", disableOutlineColor);
                    }
                }
            }

            EditorLayoutTools.SetLabelWidth(originLabelWidth);
        }
    }
}