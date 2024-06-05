
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;
using Modules.TextData.Components;

namespace Modules.TextData.Editor
{
    public sealed class GenerateWindow : SingletonEditorWindow<GenerateWindow>
    {
        //----- params -----

        public const string WindowTitle = "TextData";

        //----- field -----

        private TextType type = TextType.Internal;

        private TextDataSource selection = null;

		private TextDataConfig config = null;

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
			var languageManager = LanguageManager.Instance;

            var generateInfos = languageManager.Current;

            if (generateInfos != null)
            {
				config = TextDataConfig.Instance;

				EditorGUILayout.Separator();

	            // タイプ選択.
                if (config.EnableExternal)
                {
    	            DrawTextDataTypeGUI();
                }

                // エクセル制御.
                DrawExcelControlGUI();

	            // 生成.
	            DrawGenerateGUI();
			}
			else
			{
				EditorGUILayout.HelpBox("No language selected.\nPlease select language.", MessageType.Error);
			}
		}

        // タイプGUI描画.
        private void DrawTextDataTypeGUI()
        {
            var enumValues = Enum.GetValues(typeof(TextType)).Cast<TextType>().ToArray();

            var index = enumValues.IndexOf(x => x == type);

            var tabItems = enumValues.Select(x => x.ToString()).ToArray();

            EditorGUI.BeginChangeCheck();

            index = GUILayout.Toolbar(index, tabItems, "MiniButton", GUI.ToolbarButtonSize.Fixed);

            if (EditorGUI.EndChangeCheck())
            {
                type = enumValues.ElementAtOrDefault(index);

                selection = null;
            }

            GUILayout.Space(4f);
        }
        
        // 生成制御GUI描画.
        private void DrawGenerateGUI()
        {
			var languageManager = LanguageManager.Instance;

            var languageInfo = languageManager.Current;

            TextDataSource[] sources = null;

            switch (type)
            {
                case TextType.Internal:
                    sources = config.Internal.Source;
                    break;

                case TextType.External:
                    sources = config.External.Source;
                    break;
            }

            EditorLayoutTools.Title("Asset");
            
            GUILayout.Space(4f);

            // 生成制御.
            using (new DisableScope(languageInfo == null || sources.IsEmpty()))
            {
                if (GUILayout.Button("Generate"))
                {
                    TextDataGenerator.Generate(type, languageInfo, true);

                    UnityConsole.Info("TextData generate finish.");

                    Repaint();
                }
            }

            GUILayout.Space(4f);
        }

        // エクセル制御GUI描画.
        private void DrawExcelControlGUI()
        {
            EditorLayoutTools.Title("Excel");

            TextDataSource[] sources = null;

            switch (type)
            {
                case TextType.Internal:
                    sources = config.Internal.Source;
                    break;

                case TextType.External:
                    sources = config.External.Source;
                    break;
            }

            if (sources.IsEmpty()){ return; }

            if (selection == null)
            {
                selection = sources.FirstOrDefault();
            }

            if (1 < sources.Length)
            {
                GUILayout.Space(4f);

                var labels = sources.Select(x => x.DisplayName).ToArray();
                var selectIndex = selection != null ? sources.IndexOf(x => x.DisplayName == selection.DisplayName) : -1;

                EditorGUI.BeginChangeCheck();

                var index = EditorGUILayout.Popup(selectIndex, labels);

                if (EditorGUI.EndChangeCheck())
                {
                    selection = sources.ElementAtOrDefault(index);
                }
            }
            else
            {
                selection = sources.FirstOrDefault();
            }

            GUILayout.Space(4f);

            if (selection != null)
            {
                var excelFilePath = selection.GetExcelPath();

                var excelFileExists = File.Exists(excelFilePath);

                using (new DisableScope(!excelFileExists))
                {
                    if (GUILayout.Button("Open"))
                    {
                        TextDataExcel.Open(selection);
                    }
                }

                GUILayout.Space(4f);

                var isLock = TextDataExcel.IsExcelFileLocked(selection);

                using (new DisableScope(isLock))
                {
                    if (GUILayout.Button("Import"))
                    {
                        Import(selection).Forget();
                    }
                }

                GUILayout.Space(4f);

                using (new DisableScope(!excelFileExists))
                {
                    if (GUILayout.Button("Export"))
                    {
                        Export(selection).Forget();
                    }
                }

                GUILayout.Space(4f);
            }
        }

        private async UniTask Import(TextDataSource source)
        {
            try
            {
                TextDataExcel.Importing = true;

                await TextDataExcel.Import(source, true);

                UnityConsole.Info("TextData import record finish.");
            }
            finally
            {
                TextDataExcel.Importing = false;
            }
        }

        private async UniTask Export(TextDataSource source)
        {
            try
            {
                TextDataExcel.Exporting = true;

                await TextDataExcel.Export(source, true);

                UnityConsole.Info("TextData export record finish.");
            }
            finally
            {
                TextDataExcel.Exporting = false;
            }
        }
	}
}
