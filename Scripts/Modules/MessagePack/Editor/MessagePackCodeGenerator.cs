
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UniRx;
using Extensions;

namespace Modules.MessagePack
{
    public static class MessagePackCodeGenerator
    {
        //----- params -----

        private const string CodeGenerateCommand = "mpc";

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool Generate()
        {
            var isSuccess = false;

            var generateInfo = new MessagePackCodeGenerateInfo();

            var lastUpdateTime = DateTime.MinValue;

            if (File.Exists(generateInfo.CsFilePath))
            {
                var fileInfo = new FileInfo(generateInfo.CsFilePath);

                lastUpdateTime = fileInfo.LastWriteTime;
            }

            var fileName = string.Empty;
            var arguments = string.Empty;

            #if UNITY_EDITOR_WIN

            fileName = "mpc";
            arguments = generateInfo.CommandLineArguments;

            #endif

            #if UNITY_EDITOR_OSX

            fileName = "/bin/bash";
            arguments = string.Format("-c mpc {0}", generateInfo.CommandLineArguments);

            #endif

            var codeGenerateResult = ProcessUtility.Start(fileName, arguments);

            if (codeGenerateResult.Item1 == 0)
            {
                isSuccess = CsFileUpdate(generateInfo, lastUpdateTime);
            }

            OutputGenerateLog(isSuccess, generateInfo);

            if (!isSuccess)
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
            var isSuccess = false;
            
            var generateInfo = new MessagePackCodeGenerateInfo();

            var lastUpdateTime = DateTime.MinValue;
            
            if (File.Exists(generateInfo.CsFilePath))
            {
                var fileInfo = new FileInfo(generateInfo.CsFilePath);

                lastUpdateTime = fileInfo.LastWriteTime;
            }

            var fileName = string.Empty;
            var arguments = string.Empty;

            #if UNITY_EDITOR_WIN

            fileName = CodeGenerateCommand;
            arguments = generateInfo.CommandLineArguments;

            #endif

            #if UNITY_EDITOR_OSX

            fileName = "/bin/bash";
            arguments = string.Format("-c {0}{1}", CodeGenerateCommand, generateInfo.CommandLineArguments);

            #endif

            var codeGenerateTask = ProcessUtility.StartAsync(fileName, arguments);

            while (!codeGenerateTask.IsCompleted)
            {
                yield return null;
            }

            if (codeGenerateTask.Result.Item1 == 0)
            {
                isSuccess = CsFileUpdate(generateInfo, lastUpdateTime);
            }

            OutputGenerateLog(isSuccess, generateInfo);

            if (!isSuccess)
            {
                using (new DisableStackTraceScope())
                {
                    Debug.LogError(codeGenerateTask.Result.Item2);
                }
            }

            observer.OnNext(isSuccess);
            observer.OnCompleted();
        }

        private static void ImportGeneratedCsFile(MessagePackCodeGenerateInfo generateInfo)
        {
            var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(generateInfo.CsFilePath);

            if (File.Exists(generateInfo.CsFilePath))
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static bool CsFileUpdate(MessagePackCodeGenerateInfo generateInfo, DateTime lastUpdateTime)
        {
            var isCsFileUpdate = false;

            if (File.Exists(generateInfo.CsFilePath))
            {
                var fileInfo = new FileInfo(generateInfo.CsFilePath);

                isCsFileUpdate = lastUpdateTime < fileInfo.LastWriteTime;
            }

            ImportGeneratedCsFile(generateInfo);

            return isCsFileUpdate;
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
