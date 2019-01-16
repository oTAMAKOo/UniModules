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
        
        private int? selection = null;

        private static GameTextGenerateWindow<TInstance> instance = null;

        //----- property -----

        public override string WindowTitle { get { return "GameText"; } }

        public override Vector2 WindowSize { get { return new Vector2(250f, 50f); } }

        public abstract GameTextGenerateInfo[] GenerateInfos { get; }

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

            GameTextGenerateInfo info = null;

            if (selection.HasValue)
            {
                info = selection.Value < GenerateInfos.Length ? GenerateInfos[selection.Value] : null;
            }            

            using (new DisableScope(info == null))
            {
                if (GUILayout.Button("Generate"))
                {
                    GameTextGenerater.Generate(connector, info);
                    Repaint();
                }
            }

            GUILayout.Space(3f);

            if (EditorLayoutTools.DrawHeader("Option", "GameTextGenerateWindow-Type"))
            {
                using (new ContentsScope())
                {
                    var labels = GenerateInfos.Select(x => x.Language).ToArray();

                    EditorGUI.BeginChangeCheck();

                    var index = EditorGUILayout.Popup(Prefs.selection, labels);

                    if (EditorGUI.EndChangeCheck())
                    {
                        selection = index;
                        Prefs.selection = index;
                    }
                }
            }
        }

        private void Reload()
        {
            if (!selection.HasValue && Prefs.selection != -1)
            {
                selection = Prefs.selection;
                Repaint();
            }
        }
    }
}
