﻿﻿﻿
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;
using Extensions;
using Extensions.Devkit;
using System.Threading;

namespace Modules.GameText.Editor
{
    public class GameTextGenerateWindow : EditorWindow
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
            var generateInfos = GameTextLanguage.Infos;

            if (generateInfos == null) { return; }

            Reload();

            EditorGUILayout.Separator();

            GameTextLanguage.Info info = null;

            if (selection.HasValue)
            {
                info = generateInfos.ElementAtOrDefault(selection.Value);
            }

            using (new DisableScope(info == null))
            {
                if (GUILayout.Button("Generate"))
                {
                    GameTextGenerater.Generate(info);

                    Repaint();
                }
            }

            EditorGUILayout.Separator();

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
