
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
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

        private ContentType contentType = ContentType.Embedded;

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
	            DrawTextDataTypeGUI();

	            // 生成.
	            DrawGenerateGUI();

	            // エクセル制御.
	            DrawExcelControlGUI();
			}
			else
			{
				EditorGUILayout.HelpBox("No language selected.\nPlease select language.", MessageType.Error);
			}
		}

        // タイプGUI描画.
        private void DrawTextDataTypeGUI()
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
			var languageManager = LanguageManager.Instance;

            var languageInfo = languageManager.Current;

            EditorLayoutTools.Title("Asset");
            
            GUILayout.Space(4f);
            
            // 生成制御.
            using (new DisableScope(languageInfo == null))
            {
                if (GUILayout.Button("Generate"))
                {
                    TextDataGenerator.Generate(contentType, languageInfo);

                    UnityConsole.Info("TextData generate finish.");

                    Repaint();
                }
            }

            GUILayout.Space(4f);
        }

        // エクセル制御GUI描画.
        private void DrawExcelControlGUI()
        {
            TextDataConfig.GenerateAssetSetting setting = null;

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
					TextDataExcel.Open(setting);
                }
            }

            GUILayout.Space(4f);

            var isLock = TextDataExcel.IsExcelFileLocked(setting);

            using (new DisableScope(isLock))
            {
                if (GUILayout.Button("Import"))
                {
                    var nowait = TextDataExcel.Import(contentType, true);

                    UnityConsole.Info("TextData import record finish.");
                }
            }

            GUILayout.Space(4f);

            using (new DisableScope(!excelFileExists))
            {
                if (GUILayout.Button("Export"))
                {
                    var nowait = TextDataExcel.Export(contentType, true);

                    UnityConsole.Info("TextData export record finish.");
                }
            }

            GUILayout.Space(4f);
        }
	}
}
