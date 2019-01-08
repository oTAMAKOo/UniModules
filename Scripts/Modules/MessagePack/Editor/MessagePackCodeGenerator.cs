
using UnityEditor;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Extensions;

using Debug = UnityEngine.Debug;

namespace Modules.MessagePack
{
    public static class MessagePackCodeGenerator
    {
        //----- params -----
        
        private const string CSProjExtension = ".csproj";

        //----- field -----

        //----- property -----

        //----- method -----
        
        public static bool Compile()
        {
            var result = false;

            var messagePackConfig = MessagePackConfig.Instance;

            var csprojName = string.Empty;

            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            #if UNITY_2018_3_OR_NEWER

            csprojName = "Assembly-CSharp";

            #else

            csprojName = UnityPathUtility.GetProjectName();

            #endif

            var csproj = PathUtility.Combine(projectFolder, string.Format("{0}{1}", csprojName, CSProjExtension));
            var generatePath = PathUtility.Combine(messagePackConfig.ScriptExportDir, messagePackConfig.ExportScriptName);

            var arguments = string.Format(" --input {0} --output {1}", csproj, generatePath);

            if (EditorUserBuildSettings.development)
            {
                arguments += " --usemapmode";
            }

            try
            {
                var processStartInfo = new ProcessStartInfo(messagePackConfig.CompilerPath, arguments);

                // エラー出力をリダイレクト.
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;

                // シェル実行しない.
                processStartInfo.UseShellExecute = false;

                // ウィンドウ非表示.
                processStartInfo.CreateNoWindow = true;                

                // 実行ファイルを指定して実行.
                using (var compileProcess = new Process())
                {
                    var compileLog = new StringBuilder();

                    compileProcess.StartInfo = processStartInfo;

                    DataReceivedEventHandler processOutputDataReceived = (sender, e) =>
                    {
                        compileLog.AppendLine(e.Data);
                    };

                    compileProcess.OutputDataReceived += processOutputDataReceived;

                    // タイムアウト時間 (120秒).
                    var timeout = TimeSpan.FromSeconds(120);

                    //起動.
                    compileProcess.Start();

                    compileProcess.BeginOutputReadLine();

                    // 結果待ち.
                    compileProcess.WaitForExit((int)timeout.TotalMilliseconds);

                    if (compileProcess.ExitCode == 1)
                    {
                        Debug.LogErrorFormat("MessagePack code geneation failed.\n\n{0}", compileLog.ToString());
                    }
                    else
                    {
                        Debug.LogFormat("Generate: {0}", generatePath);

                        result = true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
            }

            return result;
        }
    }
}
