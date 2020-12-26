
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Reflection;
using UniRx;
using Extensions;

namespace Modules.MessagePack
{
    public static class MessagePackCodeGenerator
    {
        //----- params -----

        private const string CSProjExtension = ".csproj";

        private sealed class GenerateInfo
        {
            public string codeGeneratorPath = null;
            public string commnadLineArguments = null;
            public string csFilePath = null;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static IObservable<Tuple<bool, string>> FindDotnet()
        {
            Tuple<bool, string> result = null;

            return ProcessUtility.InvokeStartAsync("dotnet", "--version").ToObservable()
                .Do(x => result = Tuple.Create(true, x.Item2))
                .DoOnError(x => result = Tuple.Create(false, (string)null))
                .Select(_ => result);
        }

        public static IObservable<bool> Generate()
        {
            return Observable.FromMicroCoroutine<bool>(observer => GenerateInternal(observer));
        }

        private static IEnumerator GenerateInternal(IObserver<bool> observer)
        {
            var isSuccess = false;
            
            var messagePackConfig = MessagePackConfig.Instance;

            var lastUpdateTime = DateTime.MinValue;

            var csFilePath = GetScriptGeneratePath(messagePackConfig);

            if (File.Exists(csFilePath))
            {
                var fileInfo = new FileInfo(csFilePath);

                lastUpdateTime = fileInfo.LastWriteTime;
            }

            var generateCodeYield = Observable.FromMicroCoroutine<GenerateInfo>(x => GenerateCodeCore(x, messagePackConfig)).ToYieldInstruction(false);

            while(!generateCodeYield.IsDone)
            {
                yield return null;
            }

            if (generateCodeYield.HasResult)
            {
                var info = generateCodeYield.Result;

                var isCsFileUpdate = false;

                if (File.Exists(csFilePath))
                {
                    var fileInfo = new FileInfo(csFilePath);

                    isCsFileUpdate = lastUpdateTime < fileInfo.LastWriteTime;
                }

                using (new DisableStackTraceScope())
                {
                    var logBuilder = new StringBuilder();

                    logBuilder.AppendLine();
                    logBuilder.AppendLine();
                    logBuilder.AppendFormat("MessagePack file : {0}", info.csFilePath).AppendLine();
                    logBuilder.AppendLine();
                    logBuilder.AppendFormat("Command:").AppendLine();
                    logBuilder.AppendLine(info.codeGeneratorPath + info.commnadLineArguments);

                    if (isCsFileUpdate)
                    {
                        isSuccess = true;
                        
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
            else if(generateCodeYield.HasError)
            {
                using (new DisableStackTraceScope())
                {
                    Debug.LogException(generateCodeYield.Error);
                }
            }

            observer.OnNext(isSuccess);
            observer.OnCompleted();
        }

        private static IEnumerator GenerateCodeCore(IObserver<GenerateInfo> observer, MessagePackConfig messagePackConfig)
        {
            var generateInfo = new GenerateInfo();

            //------ Solution同期 ------

            var unitySyncVS = Type.GetType("UnityEditor.SyncVS,UnityEditor");

            var syncSolution = unitySyncVS.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);

            syncSolution.Invoke(null, null);

            //------ csproj検索 ------

            var csprojPath = string.Empty;

            var projectFolder = UnityPathUtility.GetProjectFolderPath();

            var csprojNames = new string[] { "Assembly-CSharp", UnityPathUtility.GetProjectName() };

            foreach (var csprojName in csprojNames)
            {
                var path = PathUtility.Combine(projectFolder, string.Format("{0}{1}", csprojName, CSProjExtension));

                if (File.Exists(path))
                {
                    csprojPath = path;
                    break;
                }
            }

            if (!File.Exists(csprojPath))
            {
                observer.OnError(new FileNotFoundException(string.Format("csproj file not found.\n{0}", csprojPath)));
            }

            //------ mpc ------

            var codeGeneratorPath = messagePackConfig.CodeGeneratorPath;

            if (!File.Exists(codeGeneratorPath))
            {
                observer.OnError(new FileNotFoundException(string.Format("MessagePack Code Generator file not found.\n{0}", codeGeneratorPath)));
                yield break;
            }

            generateInfo.codeGeneratorPath = codeGeneratorPath;

            #if UNITY_EDITOR_OSX
            {
                //------ mpc権限変更 ------
            
                var task = ProcessUtility.InvokeStartAsync("/bin/bash", string.Format("-c 'chmod 755 {0}'", codeGeneratorPath));

                while (!task.IsCompleted)
                {
                    yield return null;
                }

                //------ msbuild ------

                var environmentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

                var path = string.Format("{0}:{1}", environmentPath, MessagePackConfig.Prefs.msbuildPath);

                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
            }
            #endif

            //------ 出力先 ------

            var generatePath = GetScriptGeneratePath(messagePackConfig);

            generateInfo.csFilePath = generatePath;

            //------ mpc実行 ------

            var commnadLineArguments = string.Empty;
            
            commnadLineArguments += $" --input { ReplaceCommnadLinePathSeparator(csprojPath) }";

            commnadLineArguments += $" --output { ReplaceCommnadLinePathSeparator(generatePath) }";

            if (messagePackConfig.UseMapMode)
            {
                commnadLineArguments += " --usemapmode";
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ResolverNameSpace))
            {
                commnadLineArguments += $" --namespace {messagePackConfig.ResolverNameSpace}";
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ResolverName))
            {
                commnadLineArguments += $" --resolverName {messagePackConfig.ResolverName}";
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ConditionalCompilerSymbols))
            {
                commnadLineArguments += $" --conditionalSymbol {messagePackConfig.ConditionalCompilerSymbols}";
            }

            generateInfo.commnadLineArguments = commnadLineArguments;

            // 実行.
            {
                var task = ProcessUtility.InvokeStartAsync(codeGeneratorPath, commnadLineArguments);

                while (!task.IsCompleted)
                {
                    yield return null;
                }

                if (task.Result.Item1 == 1)
                {
                    observer.OnError(new Exception(task.Result.Item2));
                }
            }

            var csFilePath = UnityPathUtility.ConvertFullPathToAssetPath(generatePath);

            if (File.Exists(generatePath))
            {
                AssetDatabase.ImportAsset(csFilePath, ImportAssetOptions.ForceUpdate);
            }

            observer.OnNext(generateInfo);
            observer.OnCompleted();
        }

        private static string ReplaceCommnadLinePathSeparator(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string GetScriptGeneratePath(MessagePackConfig messagePackConfig)
        {
            return PathUtility.Combine(messagePackConfig.ScriptExportDir, messagePackConfig.ExportScriptName);
        }
    }
}
