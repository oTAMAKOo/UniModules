
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

        public static Tuple<int, string> Start(string fileName, string arguments)
        {
            var process = new Process();

            var output = new StringBuilder();

            try
            {
                process.StartInfo = CreateProcessStartInfo(fileName, arguments);

                DataReceivedEventHandler processOutputDataReceived = (sender, e) =>
                {
                    output.AppendLine(e.Data);
                };

                process.OutputDataReceived += processOutputDataReceived;

                process.Start();

                process.BeginOutputReadLine();

                process.WaitForExit((int)TimeOut.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return Tuple.Create(1, ex.Message);
            }

            var exitCode = process.ExitCode;

            process.Dispose();
            process = null;

            return Tuple.Create(exitCode, output.ToString());
        }

        public static Task<Tuple<int, string>> StartAsync(string fileName, string arguments)
        {
            var process = new Process();

            var tcs = new TaskCompletionSource<Tuple<int, string>>();

            try
            {
                process.StartInfo = CreateProcessStartInfo(fileName, arguments);

                process.EnableRaisingEvents = true;

                process.Exited += (object sender, EventArgs e) =>
                {
                    var exitCode = process.ExitCode;
                    var output = string.Empty;

                    output = exitCode == 0 ? process.StandardOutput.ReadToEnd() : process.StandardError.ReadToEnd();

                    process.Dispose();
                    process = null;

                    tcs.TrySetResult(Tuple.Create(exitCode, output));
                };

                process.Start();
            }
            catch (Exception ex)
            {
                return Task.FromException<Tuple<int, string>>(ex);
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

                // エラー出力をリダイレクト.
                RedirectStandardOutput = true,
                RedirectStandardError = true,

                // シェル実行しない.
                UseShellExecute = false,
            };

            return processStartInfo;
        }
    }
}
