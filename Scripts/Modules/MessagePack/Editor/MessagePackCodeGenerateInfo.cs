
using System;
using System.IO;
using System.Reflection;
using System.Text;
using Extensions;

namespace Modules.MessagePack
{
    public sealed class MessagePackCodeGenerateInfo
    {
        //----- params -----
        
        //----- field -----

        //----- property -----

        public string CodeGenerateTarget { get; private set; }

        public string CsFilePath { get; private set; }

        public string MpcArgument { get; private set; }

        //----- method -----

        public MessagePackCodeGenerateInfo()
        {
            var messagePackConfig = MessagePackConfig.Instance;

            SyncSolution();

            CodeGenerateTarget = messagePackConfig.CodeGenerateTarget;

            CsFilePath = GetScriptGeneratePath(messagePackConfig);

            MpcArgument = CreateMpcArgument(messagePackConfig, CodeGenerateTarget, CsFilePath);
        }

        private static void SyncSolution()
        {
            var unitySyncVS = Type.GetType("UnityEditor.SyncVS,UnityEditor");

            var syncSolution = unitySyncVS.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);

            syncSolution.Invoke(null, null);
        }

        private static string GetScriptGeneratePath(MessagePackConfig messagePackConfig)
        {
            return PathUtility.Combine(messagePackConfig.ScriptExportDir, messagePackConfig.ExportScriptName);
        }

        private static string CreateMpcArgument(MessagePackConfig messagePackConfig, string input, string output)
        {
            var processArgument = new StringBuilder();

            processArgument.AppendFormat(" --input \"{0}\"", ReplacePathSeparator(input));

            processArgument.AppendFormat(" --output \"{0}\"", ReplacePathSeparator(output));

            if (messagePackConfig.UseMapMode)
            {
                processArgument.Append(" --usemapmode");
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ResolverNameSpace))
            {
                processArgument.AppendFormat(" --namespace {0}", messagePackConfig.ResolverNameSpace);
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ResolverName))
            {
                processArgument.AppendFormat(" --resolverName {0}", messagePackConfig.ResolverName);
            }

            if (!string.IsNullOrEmpty(messagePackConfig.ConditionalCompilerSymbols))
            {
                processArgument.AppendFormat(" --conditionalSymbol {0}", messagePackConfig.ConditionalCompilerSymbols);
            }

            return processArgument.ToString();
        }

        private static string ReplacePathSeparator(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
