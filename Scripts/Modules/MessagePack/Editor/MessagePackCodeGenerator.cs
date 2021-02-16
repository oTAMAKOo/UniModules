
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
        
        #if UNITY_EDITOR_WIN

        private const string CodeGenerateCommand = "mpc";

        #endif

        #if UNITY_EDITOR_OSX

        private const string CodeGenerateCommand = "dotnet mpc";

        #endif

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool Generate()
        {
            var generateInfo = new MessagePackCodeGenerateInfo();

            var csFileHash = GetCsFileHash(generateInfo);

            var fileName = string.Empty;
            var arguments = string.Empty;

            #if UNITY_EDITOR_WIN

            fileName = CodeGenerateCommand;
            arguments = generateInfo.CommandLineArguments;

            #endif

            #if UNITY_EDITOR_OSX

            fileName = "/bin/bash";
            arguments = string.Format("-c {0} {1}", CodeGenerateCommand, generateInfo.CommandLineArguments);

            #endif

            var codeGenerateResult = ProcessUtility.Start(fileName, arguments);
            
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

            var fileName = string.Empty;
            var arguments = string.Empty;

            #if UNITY_EDITOR_WIN

            fileName = CodeGenerateCommand;
            arguments = generateInfo.CommandLineArguments;

            #endif

            #if UNITY_EDITOR_OSX

            fileName = "/bin/bash";
            arguments = string.Format("-c {0} {1}", CodeGenerateCommand, generateInfo.CommandLineArguments);

            #endif

            var codeGenerateTask = ProcessUtility.StartAsync(fileName, arguments);

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
                    Debug.LogError(codeGenerateTask.Result.Item2);
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
                logBuilder.AppendLine(CodeGenerateCommand + generateInfo.CommandLineArguments);

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
