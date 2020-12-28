
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
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
            var isSuccess = false;

            var generateInfo = new MessagePackCodeGenerateInfo();

            var lastUpdateTime = DateTime.MinValue;

            if (File.Exists(generateInfo.CsFilePath))
            {
                var fileInfo = new FileInfo(generateInfo.CsFilePath);

                lastUpdateTime = fileInfo.LastWriteTime;
            }

            #if !UNITY_EDITOR_OSX

            SetCodeGeneratorPermissions(generateInfo.CodeGeneratorPath);

            SetMsBuildPath();

            #endif

            var codeGenerateResult = ProcessUtility.Start(generateInfo.CodeGeneratorPath, generateInfo.CommandLineArguments);

            if (codeGenerateResult.Item1 == 0)
            {
                isSuccess = CsFileUpdate(generateInfo, lastUpdateTime);

                OutputGenerateLog(isSuccess, generateInfo);
            }
            else
            {
                throw new Exception(codeGenerateResult.Item2);
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

            #if !UNITY_EDITOR_OSX
            
            var setPermissionsTask = SetCodeGeneratorPermissionsAsync(generateInfo.CodeGeneratorPath);

            while (!setPermissionsTask.IsCompleted)
            {
                yield return null;
            }
            
            SetMsBuildPath();

            #endif

            var codeGenerateTask = ProcessUtility.StartAsync(generateInfo.CodeGeneratorPath, generateInfo.CommandLineArguments);

            while (!codeGenerateTask.IsCompleted)
            {
                yield return null;
            }

            if (codeGenerateTask.Result.Item1 == 0)
            {
                isSuccess = CsFileUpdate(generateInfo, lastUpdateTime);

                OutputGenerateLog(isSuccess, generateInfo);
            }
            else
            {
                observer.OnError(new Exception(codeGenerateTask.Result.Item2));
            }

            observer.OnNext(isSuccess);
            observer.OnCompleted();
        }

        private static Tuple<int, string> SetCodeGeneratorPermissions(string codeGeneratorPath)
        {
            return ProcessUtility.Start("/bin/bash", string.Format("-c 'chmod 755 {0}'", codeGeneratorPath));
        }

        private static async Task<Tuple<int, string>> SetCodeGeneratorPermissionsAsync(string codeGeneratorPath)
        {
            var result = await ProcessUtility.StartAsync("/bin/bash", string.Format("-c 'chmod 755 {0}'", codeGeneratorPath));

            return result;
        }

        private static void SetMsBuildPath()
        {
            var msbuildPath = MessagePackConfig.Prefs.msbuildPath;

            var environmentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

            var path = string.Format("{0}:{1}", environmentPath, msbuildPath);

            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
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
                logBuilder.AppendLine(generateInfo.CodeGeneratorPath + generateInfo.CommandLineArguments);

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
