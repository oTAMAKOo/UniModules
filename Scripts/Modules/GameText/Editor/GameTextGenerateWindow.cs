
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.GameText.Editor
{
    public sealed class GameTextGenerateWindow : EditorWindow
    {
        //----- params -----

        public const string WindowTitle = "GameText";

        //----- field -----

        private int? selection = null;

        private GameTextConfig config = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            var instance = EditorWindow.GetWindow<GameTextGenerateWindow>();

            if (instance != null)
            {
                instance.Initialize();
            }
        }

        private void Initialize()
        {
            titleContent = new GUIContent(WindowTitle);
            
            Show(true);
        }

        void OnGUI()
        {
            var config = GameTextConfig.Instance;

            var generateInfos = GameTextLanguage.Infos;

            if (generateInfos == null) { return; }

            Reload();

            EditorGUILayout.Separator();

            // 言語情報.

            GameTextLanguage.Info info = null;

            if (selection.HasValue)
            {
                info = generateInfos.ElementAtOrDefault(selection.Value);
            }

            // 生成制御.
            
            var enumValues = Enum.GetValues(typeof(GenerateMode)).Cast<GenerateMode>().ToArray();

            var tabItems = enumValues.Select(x => x.ToLabelName()).ToArray();

            foreach (var generateSetting in config.AseetGenerateSettings)
            {
                var label = generateSetting.Label;

                EditorLayoutTools.DrawLabelWithBackground(label);

                GUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var selection = GetGenerateMode(label, generateSetting.DefaultMode);

                    var index = enumValues.IndexOf(x => x == selection);

                    EditorGUI.BeginChangeCheck();

                    index = GUILayout.Toolbar(index, tabItems, "Button", GUI.ToolbarButtonSize.Fixed, GUILayout.Width(175f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        selection = enumValues.ElementAtOrDefault(index);

                        SetGenerateMode(label, selection);
                    }

                    using (new DisableScope(info == null))
                    {
                        if (GUILayout.Button("Generate"))
                        {
                            switch (selection)
                            {
                                case GenerateMode.FullGenerate:
                                    GameTextGenerater.Generate(generateSetting.AssetFolderPath, true, info);
                                    break;

                                case GenerateMode.OnlyAsset:
                                    GameTextGenerater.Generate(generateSetting.AssetFolderPath, false, info);
                                    break;
                            }

                            Repaint();
                        }
                    }
                }

                GUILayout.Space(2f);
            }
            
            // エクセル制御.

            if (EditorLayoutTools.DrawHeader("Excel", "GameTextGenerateWindow-Converter"))
            {
                using (new ContentsScope())
                {
                    GUILayout.Space(4f);

                    var excelFilePath = config.GetExcelPath();

                    using (new DisableScope(!File.Exists(excelFilePath)))
                    {
                        if (GUILayout.Button("Open"))
                        {
                            OpenGameTextExcel();
                        }
                    }

                    GUILayout.Space(4f);
                    
                    using (new DisableScope(IsExcelFileLocked()))
                    {
                        if (GUILayout.Button("Import"))
                        {
                            var path = config.GetImporterPath();

                            var result = ExecuteProcess(path);

                            if (result.Item1 != 0)
                            {
                                Debug.LogError(result.Item2);
                            }
                        }
                    }

                    GUILayout.Space(4f);

                    if (GUILayout.Button("Export"))
                    {
                        var path = config.GetExporterPath();

                        var result = ExecuteProcess(path);

                        if (result.Item1 != 0)
                        {
                            Debug.LogError(result.Item2);
                        }
                    }

                    GUILayout.Space(4f);
                }
            }

            EditorGUILayout.Separator();

            // 言語選択.

            var labels = generateInfos.Select(x => x.Language).ToArray();

            if (1 < labels.Length)
            {
                if (EditorLayoutTools.DrawHeader("Language", "GameTextGenerateWindow-Language"))
                {
                    using (new ContentsScope())
                    {
                        EditorGUI.BeginChangeCheck();

                        var index = EditorGUILayout.Popup(GameTextLanguage.Prefs.selection, labels);

                        if (EditorGUI.EndChangeCheck())
                        {
                            selection = index;

                            GameTextLanguage.Prefs.selection = index;

                            GameTextLoader.Reload();
                        }
                    }
                }

                EditorGUILayout.Separator();
            }
        }

        private bool IsExcelFileLocked()
        {
            var editExcelPath = config.GetExcelPath();
            
            if (!File.Exists(editExcelPath)) { return false; }

            return FileUtility.IsFileLocked(editExcelPath) ;
        }

        private void OpenGameTextExcel()
        {
            var path = config.GetExcelPath();

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

        private Tuple<int, string> ExecuteProcess(string path)
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
                    WorkingDirectory = config.GetGameTextWorkspacePath(),
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

        private string GetGenerateModePrefsKey(string labe)
        {
            return string.Format("GameTextGenerateWindow-GenerateMode-{0}", labe);
        }

        private GenerateMode GetGenerateMode(string label, GenerateMode defaultMode)
        {
            return ProjectPrefs.GetEnum(GetGenerateModePrefsKey(label), defaultMode);
        }

        private void SetGenerateMode(string label, GenerateMode mode)
        {
            ProjectPrefs.SetEnum(GetGenerateModePrefsKey(label), mode);
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
