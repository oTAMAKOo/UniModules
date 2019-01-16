﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Spreadsheet;

namespace Modules.GameText.Editor
{
    public class GameTextGenerateWindow : SpreadsheetConnectionWindow
    {
        //----- params -----

        //----- field -----
        
        private int? selection = null;
        
        //----- property -----

        public override string WindowTitle { get { return "GameText"; } }

        public override Vector2 WindowSize { get { return new Vector2(250f, 50f); } }

        //----- method -----

        public static void Open()
        {
            var instance = EditorWindow.GetWindow<GameTextGenerateWindow>();

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
            var generateInfos = GameTextLanguage.GameTextInfos;

            if (generateInfos == null) { return; }

            Reload();

            GUILayout.Space(3f);

            GameTextGenerateInfo info = null;

            if (selection.HasValue)
            {
                info = generateInfos.ElementAtOrDefault(selection.Value);
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

            var labels = generateInfos.Select(x => x.Language).ToArray();

            EditorGUI.BeginChangeCheck();

            var index = EditorGUILayout.Popup(GameTextLanguage.Prefs.selection, labels);

            if (EditorGUI.EndChangeCheck())
            {
                selection = index;
                GameTextLanguage.Prefs.selection = index;

                GameTextLoader.Reload();
            }
        }

        private void Reload()
        {
            if (!selection.HasValue && GameTextLanguage.Prefs.selection != -1)
            {
                selection = GameTextLanguage.Prefs.selection;
                Repaint();
            }
        }
    }
}
