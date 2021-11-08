
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

        private const string CSProjExtension = ".csproj";

        //----- field -----

        //----- property -----

        public string CsFilePath { get; private set; }
        public string MpcArgument { get; private set; }

        //----- method -----

        public MessagePackCodeGenerateInfo()
        {
            var messagePackConfig = MessagePackConfig.Instance;

            SyncSolution();

            var csprojPath = FindAssemblyCSharp();

            if (!File.Exists(csprojPath))
            {
                throw new FileNotFoundException(string.Format("csproj file not found.\n{0}", csprojPath));
            }

            CsFilePath = GetScriptGeneratePath(messagePackConfig);

            MpcArgument = CreateMpcArgument(messagePackConfig, csprojPath, CsFilePath);
        }

        private static void SyncSolution()
        {
            var unitySyncVS = Type.GetType("UnityEditor.SyncVS,UnityEditor");

            var syncSolution = unitySyncVS.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);

            syncSolution.Invoke(null, null);
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

        private static string CreateMpcArgument(MessagePackConfig messagePackConfig, string csprojPath, string generatePath)
        {
            var processArgument = new StringBuilder();

            processArgument.AppendFormat(" --input \"{0}\"", ReplacePathSeparator(csprojPath));

            processArgument.AppendFormat(" --output \"{0}\"", ReplacePathSeparator(generatePath));

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
