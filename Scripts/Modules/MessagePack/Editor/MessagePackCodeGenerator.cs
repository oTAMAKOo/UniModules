
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Extensions;

using Debug = UnityEngine.Debug;

namespace Modules.MessagePack
{
    public static class MessagePackCodeGenerator
    {
        //----- params -----

        private const string CSProjExtension = ".csproj";

        private const string UnhandledErrorMessage = "Unhandled Error:System.AggregateException: One or more errors occurred";

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool Generate(int retryCount)
        {
            var messagePackConfig = MessagePackConfig.Instance;

            for (var i = 0; i < retryCount; i++)
            {
                try
                {
                    var result = GenerateCode(messagePackConfig);

                    if (result)
                    {
                        Debug.LogFormat("Generate: {0}", GetScriptGeneratePath(messagePackConfig));

                        return true;
                    }
                }
                catch(Exception e)
                {
                    if (e.Message.Contains(UnhandledErrorMessage))
                    {
                        Debug.LogErrorFormat("MessagePack code generate retry. [{0}/{1}]", i + 1, retryCount);
                    }
                    else
                    {
                        Debug.LogException(e);
                        return false;
                    }
                }
            }

            return false;
        }

        private static bool GenerateCode(MessagePackConfig messagePackConfig)
        {
            //------ Solution同期 ------

            var unitySyncVS = Type.GetType("UnityEditor.SyncVS,UnityEditor");

            var syncSolution = unitySyncVS.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);

            syncSolution.Invoke(null, null);

            //------ csproj検索 ------

            var csproj = string.Empty;

            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            var csprojNames = new string[] { "Assembly-CSharp", UnityPathUtility.GetProjectName() };

            foreach (var csprojName in csprojNames)
            {
                var path = PathUtility.Combine(projectFolder, string.Format("{0}{1}", csprojName, CSProjExtension));

                if (File.Exists(path))
                {
                    csproj = path;
                    break;
                }
            }

            if (!File.Exists(csproj))
            {
                throw new FileNotFoundException("csproj file not found");
            }

            //------ mpc実行 ------

            var compilerPath = messagePackConfig.CompilerPath;

            if (File.Exists(compilerPath))
            {
                var generatePath = GetScriptGeneratePath(messagePackConfig);

                var arguments = string.Format(" --input {0} --output {1} --usemapmode", csproj, generatePath);

                #if UNITY_EDITOR_WIN

                // 実行.
                var result = ExecuteProcess(compilerPath, arguments);

                if (result.Item1 == 1)
                {
                    throw new Exception(result.Item2);
                }

                #elif UNITY_EDITOR_OSX

                // msbuildへのパスを設定.

                var environmentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

                var path = string.Format("{0}:{1}", environmentPath, MessagePackConfig.Prefs.msbuildPath);

                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);

                // mpc権限変更.
                ExecuteProcess("/bin/bash", string.Format("-c 'chmod 755 {0}'", compilerPath));

                // 実行.
                var result = ExecuteProcess(compilerPath, arguments);

                if (result.Item1 == 1)
                {
                    throw new Exception(result.Item2);
                }

                #endif 
            }
            else
            {
                throw new FileNotFoundException("MessagePack compiler not found.");
            }

            return true;
        }

        private static string GetScriptGeneratePath(MessagePackConfig messagePackConfig)
        {
            return PathUtility.Combine(messagePackConfig.ScriptExportDir, messagePackConfig.ExportScriptName);
        }

        private static Tuple<int, string> ExecuteProcess(string fileName, string arguments)
        {
            var exitCode = 0;

            // タイムアウト時間 (120秒).
            var timeout = TimeSpan.FromSeconds(120);

            // ログ.
            var compileLog = new StringBuilder();
            
            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,

                    // エラー出力をリダイレクト.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    // シェル実行しない.
                    UseShellExecute = false,

                    // ウィンドウ非表示.
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                process.StartInfo = processStartInfo;

                DataReceivedEventHandler processOutputDataReceived = (sender, e) =>
                {
                    compileLog.AppendLine(e.Data);
                };

                process.OutputDataReceived += processOutputDataReceived;

                //起動.
                process.Start();

                process.BeginOutputReadLine();

                // 結果待ち.
                process.WaitForExit((int)timeout.TotalMilliseconds);

                // 終了コード.
                exitCode = process.ExitCode;
            }
            
            return new Tuple<int, string>(exitCode, compileLog.ToString());
        }
    }
}
