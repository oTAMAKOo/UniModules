
using UnityEngine;
using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Modules.MessagePack
{
    internal static class ProcessUtility
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static Task<Tuple<int, string>> InvokeStartAsync(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo()
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

            Process process = null;

            try
            {
                process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                return Task.FromException<Tuple<int, string>>(ex);
            }

            var tcs = new TaskCompletionSource<Tuple<int, string>>();

            process.EnableRaisingEvents = true;

            process.Exited += (object sender, EventArgs e) =>
            {
                var exitCode = process.ExitCode;
                var output = string.Empty;

                if (exitCode == 0)
                {
                    output = process.StandardOutput.ReadToEnd();
                }
                else
                {
                    output = process.StandardError.ReadToEnd();
                }

                process.Dispose();
                process = null;
                
                tcs.TrySetResult(Tuple.Create(exitCode, output));
            };

            return tcs.Task;
        }
    }
}
