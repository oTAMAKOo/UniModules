
using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Extensions
{
    public sealed class ProcessExecute
    {
        //----- params -----

        private static readonly TimeSpan TimeOut = TimeSpan.FromSeconds(120);

        public sealed class Result
        {
            public int ExitCode { get; private set; }
            
            public string Output { get; private set; }
            
            public string Error { get; private set; }

            public Result(int exitCode, string output, string error)
            {
                ExitCode = exitCode;
                Output = output;
                Error = error;
            }
        }

        //----- field -----

        //----- property -----

        public string WorkingDirectory { get; set; }

        public Encoding Encoding { get; set; }

        public string Command { get; set; }

        public string Arguments { get; set; }

        public bool UseShellExecute { get; set; }

        public bool Hide { get; set; }

        public bool ErrorDialog { get; set; }

        //----- method -----

        public ProcessExecute(string command, string arguments)
        {
            Encoding = Encoding.UTF8;

            Command = command;
            Arguments = arguments;
            Hide = true;
            UseShellExecute = false;
        }
        
        public Result Start()
        {
            Result result = null;
            
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

                    result = new Result(exitCode, outputLog, errorLog);
                }
            }
            catch (Exception ex)
            {
                return new Result(1, string.Empty, ex.Message);
            }

            return result;
        }

        public Task<Result> StartAsync()
        {
            var tcs = new TaskCompletionSource<Result>();

            var outputLogBuilder = new StringBuilder();
            var errorLogBuilder = new StringBuilder();

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = CreateProcessStartInfo();

                    if (!UseShellExecute)
                    {
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
                    }

                    process.EnableRaisingEvents = true;

                    process.Exited += (object sender, EventArgs e) =>
                    {
                        var exitCode = -1;

                        try
                        {
                            var senderProcess = sender as Process;

                            if (senderProcess != null)
                            {
                                senderProcess.WaitForExit();

                                exitCode = senderProcess.ExitCode;
                            }

                        }
                        catch
                        {
                            exitCode = -2;
                        }

                        var outputLog = outputLogBuilder.ToString();
                        var errorLog = errorLogBuilder.ToString();

                        var result = new Result(exitCode, outputLog, errorLog);

                        tcs.TrySetResult(result);
                    };

                    process.Start();

                    if (!UseShellExecute)
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }

                    process.WaitForExit();

                    if (!UseShellExecute)
                    {
                        process.CancelOutputRead();
                        process.CancelErrorRead();
                    }
                }
            }
            catch (Exception ex)
            {
                return Task.FromException<Result>(ex);
            }

            return tcs.Task;
        }

        private ProcessStartInfo CreateProcessStartInfo()
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = Command,
                Arguments = Arguments,

                WorkingDirectory = WorkingDirectory,

                // シェル実行しない.
                UseShellExecute = UseShellExecute,

                // エラーダイアログ表示.
                ErrorDialog = ErrorDialog,
            };

            // シェル実行時はリダイレクト出来ない.
            if (!UseShellExecute)
            {
                processStartInfo.StandardOutputEncoding = Encoding;
                processStartInfo.RedirectStandardOutput = true;

                processStartInfo.StandardErrorEncoding = Encoding;
                processStartInfo.RedirectStandardError = true;
            }

            // 非表示.
            if (Hide)
            {
                processStartInfo.CreateNoWindow = true;
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }

            return processStartInfo;
        }
    }
}
