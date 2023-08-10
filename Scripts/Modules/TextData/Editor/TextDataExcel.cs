
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.TextData.Components;

using Debug = UnityEngine.Debug;

namespace Modules.TextData.Editor
{
    public static class TextDataExcel
    {
        //----- params -----

        public enum Mode
        {
            Import,
            Export,
        }

        public static class Prefs
        {
            public static bool outputCommand
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-outputCommand", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-outputCommand", value); }
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Open(TextDataConfig.GenerateAssetSetting setting)
        {
            var path = setting.GetExcelPath();

            if(!File.Exists(path))
            {
                Debug.LogError("TextData excel file not found.");
                return;
            }

            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = path,
                };

                process.StartInfo = processStartInfo;

                //起動.
                process.Start();
            }
        }

        public static async Task Import(ContentType contentType, bool displayConsole)
        {
            var config = TextDataConfig.Instance;

            TextDataConfig.GenerateAssetSetting setting = null;

            switch (contentType)
            {
                case ContentType.Embedded:
                    setting = config.Embedded;
                    break;

                case ContentType.Distribution:
                    setting = config.Distribution;
                    break;
            }
            
            var result = await ExecuteProcess(setting, Mode.Import, displayConsole);

            if (result.Item1 != 0)
            {
                Debug.LogError(result.Item2);
            }
        }

        public static async Task Export(ContentType contentType, bool displayConsole)
        {
            var config = TextDataConfig.Instance;

            TextDataConfig.GenerateAssetSetting setting = null;

            switch (contentType)
            {
                case ContentType.Embedded:
                    setting = config.Embedded;
                    break;

                case ContentType.Distribution:
                    setting = config.Distribution;
                    break;
            }
            
            var result = await ExecuteProcess(setting, Mode.Export, displayConsole);

            if (result.Item1 != 0)
            {
                Debug.LogError(result.Item2);
            }
        }

        public static bool IsExcelFileLocked(TextDataConfig.GenerateAssetSetting setting)
        {
            var editExcelPath = setting.GetExcelPath();
            
            if (!File.Exists(editExcelPath)) { return false; }

            return FileUtility.IsFileLocked(editExcelPath) ;
        }

        private static async Task<Tuple<int, string>> ExecuteProcess(TextDataConfig.GenerateAssetSetting setting, Mode mode, bool displayConsole)
        {
            var config = TextDataConfig.Instance;

            var arguments = new StringBuilder();

            arguments.AppendFormat("--workspace {0} ", setting.GetTextDataWorkspacePath());

            switch (mode)
            {
                case Mode.Import:
                    arguments.Append("--mode import ");
                    break;

                case Mode.Export:
                    arguments.Append("--mode export ");
                    break;
            }

            var processArguments = arguments.ToString();

            var processExecute = new ProcessExecute(config.ConverterPath, processArguments)
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
                WorkingDirectory = setting.GetTextDataWorkspacePath(),
                UseShellExecute = displayConsole,
                Hide = !displayConsole,
            };

            if (Prefs.outputCommand)
            {
                using (new DisableStackTraceScope())
                {
                    Debug.Log($"{config.ConverterPath} {processArguments}");
                }
            }

            var result = await processExecute.StartAsync();

            return Tuple.Create(result.ExitCode, result.Error);
        }
    }
}
