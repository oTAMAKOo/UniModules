
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Extensions;
using Extensions.Devkit;
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

        private GameTextLanguage.Info GetCurrentLanguageInfo()
        {
            var generateInfos = GameTextLanguage.Infos;

            GameTextLanguage.Info info = null;

            if (generateInfos.Length == 1)
            {
                info = generateInfos.First();
            }
            else
            {
                if (selection.HasValue)
                {
                    info = generateInfos.ElementAtOrDefault(selection.Value);
                }
            }

            return info;
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
            var info = GetCurrentLanguageInfo();

            EditorLayoutTools.Title("Asset");
            
            GUILayout.Space(4f);
            
            // 生成制御.
            using (new DisableScope(info == null))
            {
                if (GUILayout.Button("Generate"))
                {
                    GameTextGenerator.Generate(contentType, info);

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
                    OpenGameTextExcel(setting);
                }
            }

            GUILayout.Space(4f);

            using (new DisableScope(IsExcelFileLocked(setting)))
            {
                if (GUILayout.Button("Import"))
                {
                    var importerPath = setting.GetImporterPath();

                    var result = ExecuteProcess(importerPath, setting);

                    if (result.Item1 != 0)
                    {
                        Debug.LogError(result.Item2);
                    }
                }
            }

            GUILayout.Space(4f);

            using (new DisableScope(!excelFileExists))
            {
                if (GUILayout.Button("Export"))
                {
                    var exporterPath = setting.GetExporterPath();

                    var result = ExecuteProcess(exporterPath, setting);

                    if (result.Item1 != 0)
                    {
                        Debug.LogError(result.Item2);
                    }
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

        private bool IsExcelFileLocked(GameTextConfig.GenerateAssetSetting setting)
        {
            var editExcelPath = setting.GetExcelPath();
            
            if (!File.Exists(editExcelPath)) { return false; }

            return FileUtility.IsFileLocked(editExcelPath) ;
        }

        private void OpenGameTextExcel(GameTextConfig.GenerateAssetSetting setting)
        {
            var path = setting.GetExcelPath();

            if(!File.Exists(path))
            {
                Debug.LogError("GameText excel file not found.");
                return;
            }

            using (var process = new System.Diagnostics.Process())
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                };

                process.StartInfo = processStartInfo;

                //起動.
                process.Start();
            }
        }

        private Tuple<int, string> ExecuteProcess(string path, GameTextConfig.GenerateAssetSetting setting)
        {
            var exitCode = 0;
            
            // タイムアウト時間 (30秒).
            var timeout = TimeSpan.FromSeconds(30);

            // ログ.
            var log = new StringBuilder();

            using (var process = new System.Diagnostics.Process())
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    WorkingDirectory = setting.GetGameTextWorkspacePath(),
                    FileName = path,

                    // エラー出力をリダイレクト.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    // 起動できなかった時にエラーダイアログを表示.
                    ErrorDialog = true,

                    // シェル実行しない.
                    UseShellExecute = false,                    
                };

                process.StartInfo = processStartInfo;

                System.Diagnostics.DataReceivedEventHandler processOutputDataReceived = (sender, e) =>
                {
                    log.AppendLine(e.Data);
                };

                process.OutputDataReceived += processOutputDataReceived;

                //起動.
                process.Start();

                process.BeginOutputReadLine();

                // 結果待ち.
                process.WaitForExit((int)timeout.TotalMilliseconds);

                while (!process.HasExited)
                {
                    process.Refresh();
                    Thread.Sleep(5);
                }

                // 終了コード.
                exitCode = process.ExitCode;
            }

            return new Tuple<int, string>(exitCode, log.ToString());
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
