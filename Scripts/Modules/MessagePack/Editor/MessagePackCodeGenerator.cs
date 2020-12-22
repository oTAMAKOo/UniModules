
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UniRx;
using Extensions;

namespace Modules.MessagePack
{
    public static class MessagePackCodeGenerator
    {
        //----- params -----

        private const string CSProjExtension = ".csproj";
        
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
            var messagePackConfig = MessagePackConfig.Instance;

            Action<bool> onComplete = result =>
            {
                if (result)
                {
                    var csFilePath = GetScriptGeneratePath(messagePackConfig);

                    if (File.Exists(csFilePath))
                    {
                        Debug.LogFormat("Generate: {0}", csFilePath);
                    }
                    else
                    {
                        Debug.LogError("MessagePack code generate failed.");
                    }
                }
            };

            Action<Exception> onError = (Exception e) =>
            {
                Debug.LogException(e);
            };

            return Observable.FromMicroCoroutine<bool>(observer => GenerateCode(observer, messagePackConfig))
                .DoOnError(onError)
                .Do(onComplete);
        }

        private static IEnumerator GenerateCode(IObserver<bool> observer, MessagePackConfig messagePackConfig)
        {
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

            #if UNITY_EDITOR_OSX

            // mpc権限変更.
            ExecuteProcess("/bin/bash", string.Format("-c 'chmod 755 {0}'", codeGeneratorPath));

            #endif

            //------ msbuild ------

            #if UNITY_EDITOR_OSX

            var environmentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

            var path = string.Format("{0}:{1}", environmentPath, MessagePackConfig.Prefs.msbuildPath);

            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);

            #endif

            //------ mpc実行 ------

            var generatePath = GetScriptGeneratePath(messagePackConfig);

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

            // 実行.
            var task = ProcessUtility.InvokeStartAsync(codeGeneratorPath, commnadLineArguments);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Result.Item1 == 1)
            {
                observer.OnError(new Exception(task.Result.Item2));
            }

            var csFilePath = UnityPathUtility.ConvertFullPathToAssetPath(generatePath);

            AssetDatabase.ImportAsset(csFilePath, ImportAssetOptions.ForceUpdate);

            observer.OnNext(true);
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
