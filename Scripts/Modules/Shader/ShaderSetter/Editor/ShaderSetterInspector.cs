﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Shaders
{
    [CustomEditor(typeof(ShaderSetter), true)]
    public class ShaderSetterInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private ShaderSetter instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as ShaderSetter;

            serializedObject.Update();

            // 継承して使うのでReflectionは使わない.
            var shaderNameProperty = serializedObject.FindProperty("shaderName");

            EditorGUI.BeginChangeCheck();

            var shaderName = EditorGUILayout.DelayedTextField("ShaderName", shaderNameProperty.stringValue);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("ShaderSetterInspector Undo", instance);

                instance.Set(shaderName);
            }
        }
    }
}