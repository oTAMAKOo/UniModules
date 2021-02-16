
using UnityEngine;
using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Modules.MessagePack
{
    internal static class ProcessUtility
    {
        private static readonly TimeSpan TimeOut = TimeSpan.FromSeconds(120);

        public static Tuple<int, string, string> Start(string fileName, string arguments)
        {
            Tuple<int, string, string> result = null;
            
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = CreateProcessStartInfo(fileName, arguments);
                    
                    process.Start();

                    process.WaitForExit((int)TimeOut.TotalMilliseconds);

                    var exitCode = process.ExitCode;
                    var outputLog = process.StandardOutput.ReadToEnd();
                    var errorLog = process.StandardError.ReadToEnd();

                    result = Tuple.Create(exitCode, outputLog, errorLog);
                }
            }
            catch (Exception ex)
            {
                return Tuple.Create(1, string.Empty, ex.Message);
            }

            return result;
        }

        public static Task<Tuple<int, string, string>> StartAsync(string fileName, string arguments)
        {
            var tcs = new TaskCompletionSource<Tuple<int, string, string>>();

            var outputLogBuilder = new StringBuilder();
            var errorLogBuilder = new StringBuilder();

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = CreateProcessStartInfo(fileName, arguments);

                    DataReceivedEventHandler processOutputDataReceived = (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputLogBuilder.AppendLine(e.Data);
                        }
                    };

                    process.OutputDataReceived += processOutputDataReceived;

                    DataReceivedEventHandler processErrorDataReceived = (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorLogBuilder.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += processErrorDataReceived;

                    process.EnableRaisingEvents = true;

                    process.Exited += (object sender, EventArgs e) =>
                    {
                        var exitCode = process.ExitCode;
                        var outputLog = outputLogBuilder.ToString();
                        var errorLog = errorLogBuilder.ToString();

                        var result = Tuple.Create(exitCode, outputLog, errorLog);

                        tcs.TrySetResult(result);
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    process.CancelOutputRead();
                    process.CancelErrorRead();
                }
            }
            catch (Exception ex)
            {
                return Task.FromException<Tuple<int, string, string>>(ex);
            }

            return tcs.Task;
        }

        private static ProcessStartInfo CreateProcessStartInfo(string fileName, string arguments)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,

                // ディレクトリ.
                WorkingDirectory = Application.dataPath,

                // ウィンドウ非表示.
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,

                // 文字コード.
                StandardOutputEncoding = Encoding.GetEncoding("Shift_JIS"),
                StandardErrorEncoding = Encoding.GetEncoding("Shift_JIS"),

                // ログ出力をリダイレクト.
                RedirectStandardOutput = true,
                RedirectStandardError = true,

                // シェル実行しない.
                UseShellExecute = false,
            };

            return processStartInfo;
        }
    }
}
