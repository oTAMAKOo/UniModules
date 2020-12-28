
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;

namespace Modules.MessagePack
{
    public sealed class MessagePackCodeGenerateInfo
    {
        //----- params -----

        private const string CSProjExtension = ".csproj";

        //----- field -----

        //----- property -----

        public string CodeGeneratorPath { get; private set; }
        public string CsFilePath { get; private set; }
        public string CommandLineArguments { get; private set; }

        //----- method -----

        public MessagePackCodeGenerateInfo()
        {
            var messagePackConfig = MessagePackConfig.Instance;

            var csprojPath = FindAssemblyCSharp();

            if (!File.Exists(csprojPath))
            {
                throw new FileNotFoundException(string.Format("csproj file not found.\n{0}", csprojPath));
            }

            CodeGeneratorPath = messagePackConfig.CodeGeneratorPath;

            if (!File.Exists(CodeGeneratorPath))
            {
                throw new FileNotFoundException(string.Format("MessagePack Code Generator file not found.\n{0}", CodeGeneratorPath));
            }

            CsFilePath = GetScriptGeneratePath(messagePackConfig);

            CommandLineArguments = CreateCommandLineArguments(messagePackConfig, csprojPath, CsFilePath);
        }

        private static string FindAssemblyCSharp()
        {
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

            return csprojPath;
        }

        private static string GetScriptGeneratePath(MessagePackConfig messagePackConfig)
        {
            return PathUtility.Combine(messagePackConfig.ScriptExportDir, messagePackConfig.ExportScriptName);
        }

        private static string CreateCommandLineArguments(MessagePackConfig messagePackConfig, string csprojPath, string generatePath)
        {
            var commandLineArguments = string.Empty;

            commandLineArguments += $" --input { ReplaceCommandLinePathSeparator(csprojPath) }";

            commandLineArguments += $" --output { ReplaceCommandLinePathSeparator(generatePath) }";

            if (messagePackConfig.UseMapMode)
            {
                commandLineArguments += " --usemapmode";
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ResolverNameSpace))
            {
                commandLineArguments += $" --namespace {messagePackConfig.ResolverNameSpace}";
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ResolverName))
            {
                commandLineArguments += $" --resolverName {messagePackConfig.ResolverName}";
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ConditionalCompilerSymbols))
            {
                commandLineArguments += $" --conditionalSymbol {messagePackConfig.ConditionalCompilerSymbols}";
            }

            return commandLineArguments;
        }

        private static string ReplaceCommandLinePathSeparator(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
