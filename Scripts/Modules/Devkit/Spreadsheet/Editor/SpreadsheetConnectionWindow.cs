﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.Spreadsheet
{
    public abstract class SpreadsheetConnectionWindow : EditorWindow
    {
        //----- params -----

        //----- field -----

        protected SpreadsheetConnector connector = null;
        protected string accessCode = string.Empty;

        //----- property -----

        public abstract string WindowTitle { get; }

        public abstract Vector2 WindowSize { get; }

        //----- method -----

        protected virtual void Initialize()
        {
            minSize = WindowSize;

            titleContent = new GUIContent(WindowTitle);

            var spreadsheetConfig = SpreadsheetConfig.Instance;

            connector = new SpreadsheetConnector();
            connector.Initialize(spreadsheetConfig);

            Show(true);
        }

        void OnGUI()
        {
            if (connector == null)
            {
                Initialize();
                Repaint();
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);
            {
                var originLabelWidth = EditorLayoutTools.SetLabelWidth(85f);

                GUILayout.Space(5f);

                switch (connector.State)
                {
                    case SpreadsheetConnector.AuthenticationState.SignIn:

                        DrawGUI();

                        break;

                    case SpreadsheetConnector.AuthenticationState.SignOut:

                        if (GUILayout.Button("Get Access Code"))
                        {
                            connector.OpenAccessCodeURL();
                        }

                        break;

                    case SpreadsheetConnector.AuthenticationState.WaitingAccessCode:

                        accessCode = EditorGUILayout.TextField("Access Code", accessCode);

                        GUILayout.Space(5f);

                        if (GUILayout.Button("SignIn"))
                        {
                            connector.SignIn(accessCode);
                            GUI.FocusControl("");
                        }

                        break;
                }

                GUILayout.Space(5f);

                EditorLayoutTools.SetLabelWidth(originLabelWidth);
            }
            EditorGUI.EndDisabledGroup();
        }

        protected virtual void DrawGUI(){}
    }
}
