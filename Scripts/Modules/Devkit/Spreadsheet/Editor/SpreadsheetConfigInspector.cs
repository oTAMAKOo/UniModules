﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.Spreadsheet
{
    [CustomEditor(typeof(SpreadsheetConfig), true)]
    public class SpreadsheetConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private SpreadsheetConfig instance = null;
        private SpreadsheetConnector connector = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as SpreadsheetConfig;

            if (connector == null)
            {
                connector = new SpreadsheetConnector();
                connector.Initialize(instance);
            }

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("clientId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("clientSecret"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("redirectUri"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scope"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            using (new DisableScope(connector.State != SpreadsheetConnector.AuthenticationState.SignIn))
            {
                EditorGUILayout.Separator();

                if (GUILayout.Button("SignOut"))
                {
                    connector.SignOut();
                }
            }
        }
    }
}