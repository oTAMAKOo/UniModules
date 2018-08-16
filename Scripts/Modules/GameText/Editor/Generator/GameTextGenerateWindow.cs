﻿﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using UniRx;
using Modules.Devkit.Prefs;
using Modules.Devkit.Spreadsheet;

namespace Modules.GameText.Editor
{
    public abstract class GameTextGenerateWindow<TInstance> : SpreadsheetConnectionWindow　where TInstance : GameTextGenerateWindow<TInstance>
    {
        //----- params -----

        protected static class Prefs
        {
            public static int selection
            {
                get { return ProjectPrefs.GetInt("GameTextGenerateWindowPrefs-selection", -1); }
                set { ProjectPrefs.SetInt("GameTextGenerateWindowPrefs-selection", value); }
            }
        }

        //----- field -----
        
        private int? textColumn = null;

        private static GameTextGenerateWindow<TInstance> instance = null;

        //----- property -----

        public override string WindowTitle { get { return "GameText"; } }

        public override Vector2 WindowSize { get { return new Vector2(250f, 60f); } }

        public abstract Dictionary<string, int> ColumnTable { get; }

        //----- method -----

        public static void Open()
        {
            instance = EditorWindow.GetWindow<TInstance>();

            if (instance != null)
            {
                instance.Initialize();
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            minSize = WindowSize;
        }

        protected override void DrawGUI()
        {
            Reload();

            GUILayout.Space(3f);

            using (new DisableScope(!textColumn.HasValue))
            {
                if (GUILayout.Button("Generate"))
                {
                    GameTextGenerater.Generate(connector, textColumn.Value);
                    Repaint();
                }
            }

            GUILayout.Space(3f);

            if (EditorLayoutTools.DrawHeader("Option", "GameTextGenerateWindow-Type"))
            {
                using (new ContentsScope())
                {
                    var labels = ColumnTable.Keys.ToArray();

                    EditorGUI.BeginChangeCheck();

                    var selection = EditorGUILayout.Popup(Prefs.selection, labels);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Prefs.selection = selection;

                        textColumn = selection != -1 ? (int?)ColumnTable.GetValueOrDefault(labels[selection]) : null;
                    }
                }
            }
        }

        private void Reload()
        {
            if (!textColumn.HasValue && Prefs.selection != -1)
            {
                var labels = ColumnTable.Keys.ToArray();
                textColumn = (int?)ColumnTable.GetValueOrDefault(labels[Prefs.selection]);
                Repaint();
            }
        }
    }
}