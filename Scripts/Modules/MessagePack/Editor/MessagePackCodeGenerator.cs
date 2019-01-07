
using UnityEditor;
using System;
using System.Diagnostics;
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
        
        
        public static void Compile()
        {
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

            var arguments = string.Format("--input {0} --output {1}", csproj, generatePath);

            if (EditorUserBuildSettings.development)
            {
                arguments += " --usemapmode";
            }

            try
            {
                // 実行ファイルを指定して実行.
                using (var compileProcess = new Process())
                {
                    compileProcess.StartInfo.FileName = messagePackConfig.CompilerPath;

                    compileProcess.StartInfo.Arguments = arguments;

                    var compileLog = new StringBuilder();

                    // 標準出力をリダイレクト.
                    compileProcess.StartInfo.RedirectStandardOutput = true;

                    // シェル実行しない.
                    compileProcess.StartInfo.UseShellExecute = false;

                    // ウィンドウ非表示.
                    compileProcess.StartInfo.CreateNoWindow = false;

                    DataReceivedEventHandler processOutputDataReceived = (sender, e) =>
                    {
                        compileLog.AppendLine(e.Data).AppendLine();
                    };

                    compileProcess.OutputDataReceived += processOutputDataReceived;

                    //起動.
                    compileProcess.Start();

                    compileProcess.BeginOutputReadLine();

                    // 結果待ち.
                    compileProcess.WaitForExit();

                    if (compileProcess.ExitCode == 1)
                    {
                        Debug.LogErrorFormat("MessagePack code geneation failed.\n\n{0}", compileLog.ToString());
                    }
                    else
                    {
                        Debug.LogFormat("Generate: {0}", generatePath);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
            }
        }
    }
}
