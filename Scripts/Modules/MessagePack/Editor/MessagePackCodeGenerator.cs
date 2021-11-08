
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UniRx;
using Extensions;

namespace Modules.MessagePack
{
    public static class MessagePackCodeGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool Generate()
        {
            var generateInfo = new MessagePackCodeGenerateInfo();

            var csFilePath = generateInfo.CsFilePath;

            var csFileHash = GetCsFileHash(csFilePath);

            var processExecute = CreateMpcProcess(generateInfo);

            var codeGenerateResult = processExecute.Start();
            
            var isSuccess = codeGenerateResult.Item1 == 0;

            OutputGenerateLog(isSuccess, csFilePath, processExecute);

            if (isSuccess)
            {
                ImportGeneratedCsFile(csFilePath, csFileHash);
            }
            else
            {
                using (new DisableStackTraceScope())
                {
                    Debug.LogError(codeGenerateResult.Item2);
                }
            }

            return isSuccess;
        }

        public static IObservable<bool> GenerateAsync()
        {
            return Observable.FromMicroCoroutine<bool>(observer => GenerateInternalAsync(observer));
        }

        private static IEnumerator GenerateInternalAsync(IObserver<bool> observer)
        {
            var generateInfo = new MessagePackCodeGenerateInfo();

            var csFilePath = generateInfo.CsFilePath;

            var csFileHash = GetCsFileHash(csFilePath);

            var processExecute = CreateMpcProcess(generateInfo);

            var codeGenerateTask = processExecute.StartAsync();

            while (!codeGenerateTask.IsCompleted)
            {
                yield return null;
            }

            var isSuccess = codeGenerateTask.Result.Item1 == 0;

            OutputGenerateLog(isSuccess, csFilePath, processExecute);

            if (isSuccess)
            {
                ImportGeneratedCsFile(csFilePath, csFileHash);
            }
            else
            {
                using (new DisableStackTraceScope())
                {
                    var message = codeGenerateTask.Result.Item3;

                    if (string.IsNullOrEmpty(message))
                    {
                        message = codeGenerateTask.Result.Item2;
                    }

                    Debug.LogError(message);
                }
            }

            observer.OnNext(isSuccess);
            observer.OnCompleted();
        }

        private static ProcessExecute CreateMpcProcess(MessagePackCodeGenerateInfo generateInfo)
        {
            var messagePackConfig = MessagePackConfig.Instance;

            var command = string.Empty;
            var argument = string.Empty;

            var platform = Environment.OSVersion.Platform;

            switch (platform)
            {
                case PlatformID.Win32NT:
                    {
                        command = MessagePackConfig.Prefs.MpcPath;
                        argument = generateInfo.MpcArgument;
                    }
                    break;

                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    {
                        command = "/bin/bash";
                        argument = $"-c \"{MessagePackConfig.Prefs.MpcPath}{generateInfo.MpcArgument}\"";
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            var processExecute = new ProcessExecute(command, argument)
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
            };

            return processExecute;
        }

        private static void ImportGeneratedCsFile(string csFilePath, string csFileHash)
        {
            var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(csFilePath);

            var hash = GetCsFileHash(csFilePath);

            if (File.Exists(csFilePath))
            {
                if (csFileHash != hash)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        private static string GetCsFileHash(string csFilePath)
        {
            var hash = string.Empty;

            if (!File.Exists(csFilePath)){ return string.Empty; }

            using (var fs = new FileStream(csFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var md5 = new MD5CryptoServiceProvider();

                var bs = md5.ComputeHash(fs);

                md5.Clear();

                hash = BitConverter.ToString(bs).ToLower().Replace("-", "");
            }

            return hash;
        }

        private static void OutputGenerateLog(bool result, string csFilePath, ProcessExecute processExecute)
        {
            using (new DisableStackTraceScope())
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendFormat("MessagePack file : {0}", csFilePath).AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendFormat("Execute:").AppendLine();
                logBuilder.AppendLine($"{processExecute.Command} {processExecute.Arguments}");

                if (result)
                {
                    logBuilder.Insert(0, "MessagePack code generate success!");

                    Debug.Log(logBuilder.ToString());
                }
                else
                {
                    logBuilder.Insert(0, "MessagePack code generate failed.");

                    Debug.LogError(logBuilder.ToString());
                }
            }
        }
    }
}
