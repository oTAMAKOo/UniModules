
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

            var outputLogBuilder = new StringBuilder();
            var errorLogBuilder = new StringBuilder();

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = CreateProcessStartInfo(fileName, arguments);

                    DataReceivedEventHandler processOutputDataReceived = (sender, e) =>
                    {
                        outputLogBuilder.AppendLine(e.Data);
                    };

                    process.OutputDataReceived += processOutputDataReceived;

                    DataReceivedEventHandler processErrorDataReceived = (sender, e) =>
                    {
                        errorLogBuilder.AppendLine(e.Data);
                    };

                    process.ErrorDataReceived += processErrorDataReceived;

                    process.Start();

                    process.BeginOutputReadLine();

                    process.WaitForExit((int)TimeOut.TotalMilliseconds);

                    result = Tuple.Create(process.ExitCode, outputLogBuilder.ToString(), errorLogBuilder.ToString());
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
                        outputLogBuilder.AppendLine(e.Data);
                    };

                    process.OutputDataReceived += processOutputDataReceived;

                    DataReceivedEventHandler processErrorDataReceived = (sender, e) =>
                    {
                        errorLogBuilder.AppendLine(e.Data);
                    };

                    process.ErrorDataReceived += processErrorDataReceived;

                    process.EnableRaisingEvents = true;

                    process.Exited += (object sender, EventArgs e) =>
                    {
                        var result = Tuple.Create(process.ExitCode, outputLogBuilder.ToString(), errorLogBuilder.ToString());

                        tcs.TrySetResult(result);
                    };

                    process.Start();

                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();

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
