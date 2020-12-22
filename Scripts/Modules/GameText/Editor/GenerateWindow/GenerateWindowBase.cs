
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Extensions;
using Extensions.Devkit;

namespace Modules.GameText.Editor
{
    public abstract class GenerateWindowBase<T> : SingletonEditorWindow<T> where T : GenerateWindowBase<T>
    {
        //----- params -----
        
        //----- field -----

        protected int? selection = null;

        protected GameTextConfig config = null;

        //----- property -----

        //----- method -----

        protected GameTextLanguage.Info GetCurrentLanguageInfo()
        {
            var generateInfos = GameTextLanguage.Infos;

            GameTextLanguage.Info info = null;

            if (selection.HasValue)
            {
                info = generateInfos.ElementAtOrDefault(selection.Value);
            }

            return info;
        }

        // エクセル制御GUI描画.
        protected void ControlExcelGUIContents(GameTextConfig.GenerateAssetSetting setting)
        {
            EditorLayoutTools.ContentTitle("Excel");

            using (new ContentsScope())
            {
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

                GUILayout.Space(2f);
            }

            GUILayout.Space(2f);
        }

        // 言語選択GUI描画.
        protected void SelectLanguageGUIContents()
        {
            var generateInfos = GameTextLanguage.Infos;

            var labels = generateInfos.Select(x => x.Language).ToArray();

            if (labels.Length <= 1) { return; }

            EditorLayoutTools.ContentTitle("Language");

            using (new ContentsScope())
            {
                GUILayout.Space(2f);

                EditorGUI.BeginChangeCheck();

                var index = EditorGUILayout.Popup(GameTextLanguage.Prefs.selection, labels);

                if (EditorGUI.EndChangeCheck())
                {
                    selection = index;

                    GameTextLanguage.Prefs.selection = index;

                    GameTextLoader.Reload();
                }

                GUILayout.Space(2f);
            }

            GUILayout.Space(2f);
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

        protected void Reload()
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
