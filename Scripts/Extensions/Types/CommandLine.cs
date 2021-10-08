
using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Extensions
{
    public sealed class CommandLine
    {
        //----- params -----

        private static readonly TimeSpan TimeOut = TimeSpan.FromSeconds(120);

        //----- field -----

        //----- property -----

        public string WorkingDirectory { get; set; }

        public Encoding Encoding { get; set; }

        public string Command { get; set; }

        public string Arguments { get; set; }

        /// <summary> コマンドライン上で他のプロセスを実行するか </summary>
        public bool ProcessExecute { get; set; }

        //----- method -----

        public CommandLine(string command, string arguments)
        {
            Encoding = Encoding.GetEncoding("Shift_JIS");

            Command = command;
            Arguments = arguments;
        }
        
        public Tuple<int, string, string> Start()
        {
            Tuple<int, string, string> result = null;
            
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = CreateProcessStartInfo();
                    
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

        public Task<Tuple<int, string, string>> StartAsync()
        {
            var tcs = new TaskCompletionSource<Tuple<int, string, string>>();

            var outputLogBuilder = new StringBuilder();
            var errorLogBuilder = new StringBuilder();

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = CreateProcessStartInfo();

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
                        var exitCode = -1;

                        try
                        {
                            var senderProcess = sender as Process;

                            if (senderProcess != null)
                            {
                                exitCode = senderProcess.ExitCode;
                            }

                        }
                        catch
                        {
                            exitCode = -2;
                        }

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

        private ProcessStartInfo CreateProcessStartInfo()
        {
            var processFileName = string.Empty;
            var processArgument = new StringBuilder();

            var platform = Environment.OSVersion.Platform;

            switch (platform)
            {
                case PlatformID.Win32NT:
                    {
                        processFileName = "cmd.exe";

                        if (ProcessExecute)
                        {
                            processArgument.Append("/C ");
                        }

                        processArgument.Append(Command);
                        processArgument.Append(Arguments);
                    }
                    break;

                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    {
                        processFileName = "/bin/bash";
                    
                        if (ProcessExecute)
                        {
                            processArgument.Append("-c \"");
                        }

                        processArgument.Append(Command);
                        processArgument.Append(Arguments);
                        processArgument.Append("\"");
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = processFileName,
                Arguments = processArgument.ToString(),

                // ディレクトリ.
                WorkingDirectory = WorkingDirectory,

                // ウィンドウ非表示.
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,

                // 文字コード.
                StandardOutputEncoding = Encoding,
                StandardErrorEncoding = Encoding,

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
