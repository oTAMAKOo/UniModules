
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Extensions;

using Debug = UnityEngine.Debug;

namespace Modules.Lua.Text
{
    public static class LuaTextExcel
    {
        //----- params -----

        public enum Mode
        {
            Import,
            Export,
        }

		/// <summary> Excel拡張子 </summary>
		public const string ExcelExtension = ".xlsx";

        //----- field -----

        //----- property -----

        //----- method -----

		public static string[] FindExcelFile(string directory)
		{
			var excelPaths = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
				.Where(x => Path.GetExtension(x) == ExcelExtension)
				// 一時ファイルを除外.
				.Where(x => !Path.GetFileName(x).StartsWith("~$"))
				.Select(x => PathUtility.ConvertPathSeparator(x))
				.ToArray();

			return excelPaths;
		}

        public static void Open(string excelPath)
        {
			if(!File.Exists(excelPath))
            {
                Debug.LogErrorFormat("File not found.\n{0}", excelPath);

                return;
            }

            using (var process = new Process())
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = excelPath,
                };

                process.StartInfo = processStartInfo;

                //起動.
                process.Start();
            }
        }

        public static async Task Import(string workspace, string[] targets, bool displayConsole)
        {
			var result = await ExecuteProcess(Mode.Import, workspace, targets, displayConsole);

            if (result.Item1 != 0)
            {
                Debug.LogError(result.Item2);
            }
        }

        public static async Task Export(string workspace, string[] targets, bool displayConsole)
        {
			var result = await ExecuteProcess(Mode.Export, workspace, targets, displayConsole);

            if (result.Item1 != 0)
            {
                Debug.LogError(result.Item2);
            }
        }

		private static async Task<Tuple<int, string>> ExecuteProcess(Mode mode, string workspace, string[] targets, bool displayConsole)
        {
			var config = LuaTextConfig.Instance;

			var converterPath = config.GetConverterPath();
			var settingsIniPath = config.GetSettingsIniPath();

			var arguments = new StringBuilder();

            arguments.AppendFormat("--workspace {0} ", workspace);
			arguments.AppendFormat("--settings {0} ", settingsIniPath);

            switch (mode)
            {
                case Mode.Import:
                    arguments.Append("--mode import ");
                    break;

                case Mode.Export:
                    arguments.Append("--mode export ");
                    break;
            }

			if(targets != null && targets.Any())
			{
				arguments.AppendFormat("--targets {0} ", targets);
			}

            var processExecute = new ProcessExecute(converterPath, arguments.ToString())
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
                WorkingDirectory = workspace,
                UseShellExecute = displayConsole,
                Hide = !displayConsole,
            };

            var result = await processExecute.StartAsync();

            return Tuple.Create(result.ExitCode, result.Error);
        }
    }
}
