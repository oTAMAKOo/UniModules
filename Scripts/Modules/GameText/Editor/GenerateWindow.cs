
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public sealed class GenerateWindow : SingletonEditorWindow<GenerateWindow>
    {
        //----- params -----

        public const string WindowTitle = "GameText";

        //----- field -----

        private ContentType contentType = ContentType.Embedded;

        private int? selection = null;

        private GameTextConfig config = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        private void Initialize()
        {
            titleContent = new GUIContent(WindowTitle);

            minSize = new Vector2(250, 200f);

            Show(true);
        }

        void OnGUI()
        {
            var generateInfos = GameTextLanguage.Infos;

            if (generateInfos == null) { return; }

            Reload();

            EditorGUILayout.Separator();

            // タイプ選択.
            DrawGameTextTypeGUI();

            // 生成.
            DrawGenerateGUI();

            // エクセル制御.
            DrawExcelControlGUI();

            // 言語選択.
            DrawLanguageGUI();
        }

        // タイプGUI描画.
        private void DrawGameTextTypeGUI()
        {
            var enumValues = Enum.GetValues(typeof(ContentType)).Cast<ContentType>().ToArray();

            var index = enumValues.IndexOf(x => x == contentType);

            var tabItems = enumValues.Select(x => x.ToString()).ToArray();

            EditorGUI.BeginChangeCheck();

            index = GUILayout.Toolbar(index, tabItems, "MiniButton", GUI.ToolbarButtonSize.Fixed);

            if (EditorGUI.EndChangeCheck())
            {
                contentType = enumValues.ElementAtOrDefault(index);
            }

            GUILayout.Space(4f);
        }

        // 生成制御GUI描画.
        private void DrawGenerateGUI()
        {
            var languageInfo = GameTextLanguage.GetCurrentInfo();

            EditorLayoutTools.Title("Asset");
            
            GUILayout.Space(4f);
            
            // 生成制御.
            using (new DisableScope(languageInfo == null))
            {
                if (GUILayout.Button("Generate"))
                {
                    GameTextGenerator.Generate(contentType, languageInfo);

                    UnityConsole.Info("GameText generate finish.");

                    Repaint();
                }
            }

            GUILayout.Space(4f);
        }

        // エクセル制御GUI描画.
        private void DrawExcelControlGUI()
        {
            GameTextConfig.GenerateAssetSetting setting = null;

            switch (contentType)
            {
                case ContentType.Embedded:
                    setting = config.Embedded;
                    break;

                case ContentType.Distribution:
                    setting = config.Distribution;
                    break;
            }

            EditorLayoutTools.Title("Excel");
            
            GUILayout.Space(4f);

            var excelFilePath = setting.GetExcelPath();

            var excelFileExists = File.Exists(excelFilePath);

            using (new DisableScope(!excelFileExists))
            {
                if (GUILayout.Button("Open"))
                {
                    GameTxetExcel.Open(setting);
                }
            }

            GUILayout.Space(4f);

            var isLock = GameTxetExcel.IsExcelFileLocked(setting);

            using (new DisableScope(isLock))
            {
                if (GUILayout.Button("Import"))
                {
                    var nowait = GameTxetExcel.Import(contentType);

                    UnityConsole.Info("GameText import record finish.");
                }
            }

            GUILayout.Space(4f);

            using (new DisableScope(!excelFileExists))
            {
                if (GUILayout.Button("Export"))
                {
                    var nowait = GameTxetExcel.Export(contentType);

                    UnityConsole.Info("GameText export record finish.");
                }
            }

            GUILayout.Space(4f);
        }

        // 言語選択GUI描画.
        private void DrawLanguageGUI()
        {
            var generateInfos = GameTextLanguage.Infos;

            var labels = generateInfos.Select(x => x.Language).ToArray();

            if (labels.Length <= 1) { return; }

            EditorLayoutTools.Title("Language");
            
            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();

            var index = EditorGUILayout.Popup(GameTextLanguage.Prefs.selection, labels);

            if (EditorGUI.EndChangeCheck())
            {
                selection = index;

                GameTextLanguage.Prefs.selection = index;

                GameTextLoader.Reload();
            }

            GUILayout.Space(4f);
        }

        private void Reload()
        {
            config = GameTextConfig.Instance;

            if (!selection.HasValue && GameTextLanguage.Prefs.selection != -1)
            {
                selection = GameTextLanguage.Prefs.selection;

                Repaint();
            }            
        }
    }
}
