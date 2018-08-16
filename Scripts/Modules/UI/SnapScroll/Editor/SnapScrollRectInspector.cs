﻿﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI
{
    [CustomEditor(typeof(SnapScrollRect), true)]
    public class SnapScrollRectInspector : ScrollRectEditor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fitRange"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}