
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Extensions;
using Modules.Devkit.Console;
using Modules.GameText.Components;

using Debug = UnityEngine.Debug;

namespace Modules.GameText.Editor
{
    public static class GameTxetExcel
    {
        //----- params -----

        public enum Mode
        {
            Import,
            Export,
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Open(GameTextConfig.GenerateAssetSetting setting)
        {
            var path = setting.GetExcelPath();

            if(!File.Exists(path))
            {
                Debug.LogError("GameText excel file not found.");
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

        public static async Task Import(ContentType contentType)
        {
            var config = GameTextConfig.Instance;

            GameTextConfig.GenerateAssetSetting setting = null;

            switch (contentType)
            {
                case ContentType.Embedded:
                    setting = config.Embedded;
                    break;

                case ContentType.Distribution:
                    setting = config.Distribution;
                    break;
            }
            
            var result = await ExecuteProcess(setting, Mode.Import);

            if (result.Item1 != 0)
            {
                Debug.LogError(result.Item2);
            }
        }

        public static async Task Export(ContentType contentType)
        {
            var config = GameTextConfig.Instance;

            GameTextConfig.GenerateAssetSetting setting = null;

            switch (contentType)
            {
                case ContentType.Embedded:
                    setting = config.Embedded;
                    break;

                case ContentType.Distribution:
                    setting = config.Distribution;
                    break;
            }
            
            var result = await ExecuteProcess(setting, Mode.Export);

            if (result.Item1 != 0)
            {
                Debug.LogError(result.Item2);
            }
        }

        public static bool IsExcelFileLocked(GameTextConfig.GenerateAssetSetting setting)
        {
            var editExcelPath = setting.GetExcelPath();
            
            if (!File.Exists(editExcelPath)) { return false; }

            return FileUtility.IsFileLocked(editExcelPath) ;
        }

        private static async Task<Tuple<int, string>> ExecuteProcess(GameTextConfig.GenerateAssetSetting setting, Mode mode)
        {
            var config = GameTextConfig.Instance;

            var arguments = new StringBuilder();

            arguments.AppendFormat("--workspace {0} ", setting.GetGameTextWorkspacePath());

            switch (mode)
            {
                case Mode.Import:
                    arguments.Append("--mode import ");
                    break;

                case Mode.Export:
                    arguments.Append("--mode export ");
                    break;
            }

            var processExecute = new ProcessExecute(config.ConverterPath, arguments.ToString())
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
                WorkingDirectory = setting.GetGameTextWorkspacePath(),
            };

            var result = await processExecute.StartAsync();

            return Tuple.Create(result.Item1, result.Item3);
        }
    }
}
