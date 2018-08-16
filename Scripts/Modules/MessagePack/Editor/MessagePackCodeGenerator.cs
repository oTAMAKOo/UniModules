﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
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
        
        
        public static void Compile()
        {
            var messagePackConfig = MessagePackConfig.Instance;

            var projectFolder = UnityPathUtility.GetProjectFolderPath();
            var projectName = UnityPathUtility.GetProjectName();
                
            var csproj = PathUtility.Combine(projectFolder, string.Format("{0}{1}", projectName, CSProjExtension));
            var generatePath = PathUtility.Combine(messagePackConfig.ScriptExportDir, messagePackConfig.ExportScriptName);

            var option = string.Format("--input {0} --output {1}", csproj, generatePath);

            if (EditorUserBuildSettings.development)
            {
                option += " --usemapmode";
            }

            // 実行ファイルを指定して実行
            var compileProcess = System.Diagnostics.Process.Start(messagePackConfig.CompilerPath, option);

            compileProcess.WaitForExit();

            Debug.LogFormat("Generate: {0}", generatePath);
        }
    }
}