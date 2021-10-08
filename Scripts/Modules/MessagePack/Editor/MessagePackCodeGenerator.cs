
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

        private const string MpcCommand = "mpc";
        
        //----- field -----

        //----- property -----

        //----- method -----

        public static bool Generate()
        {
            var generateInfo = new MessagePackCodeGenerateInfo();

            var csFileHash = GetCsFileHash(generateInfo);

            var commandLineProcess = new ProcessExecute(MpcCommand, generateInfo.CommandLineArguments)
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
            };

            var codeGenerateResult = commandLineProcess.Start();
            
            var isSuccess = codeGenerateResult.Item1 == 0;

            OutputGenerateLog(isSuccess, generateInfo);

            if (isSuccess)
            {
                ImportGeneratedCsFile(generateInfo, csFileHash);
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

            var csFileHash = GetCsFileHash(generateInfo);

            var commandLineProcess = new ProcessExecute(MpcCommand, generateInfo.CommandLineArguments)
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
            };
            
            var codeGenerateTask = commandLineProcess.StartAsync();

            while (!codeGenerateTask.IsCompleted)
            {
                yield return null;
            }

            var isSuccess = codeGenerateTask.Result.Item1 == 0;

            OutputGenerateLog(isSuccess, generateInfo);

            if (isSuccess)
            {
                ImportGeneratedCsFile(generateInfo, csFileHash);
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

        private static void ImportGeneratedCsFile(MessagePackCodeGenerateInfo generateInfo, string csFileHash)
        {
            var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(generateInfo.CsFilePath);

            var hash = GetCsFileHash(generateInfo);

            if (File.Exists(generateInfo.CsFilePath))
            {
                if (csFileHash != hash)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        private static string GetCsFileHash(MessagePackCodeGenerateInfo generateInfo)
        {
            var hash = string.Empty;

            if (!File.Exists(generateInfo.CsFilePath)){ return string.Empty; }

            using (var fs = new FileStream(generateInfo.CsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var md5 = new MD5CryptoServiceProvider();

                var bs = md5.ComputeHash(fs);

                md5.Clear();

                hash = BitConverter.ToString(bs).ToLower().Replace("-", "");
            }

            return hash;
        }

        private static void OutputGenerateLog(bool result, MessagePackCodeGenerateInfo generateInfo)
        {
            using (new DisableStackTraceScope())
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendFormat("MessagePack file : {0}", generateInfo.CsFilePath).AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendFormat("Command:").AppendLine();
                logBuilder.AppendLine(MpcCommand + generateInfo.CommandLineArguments);

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
