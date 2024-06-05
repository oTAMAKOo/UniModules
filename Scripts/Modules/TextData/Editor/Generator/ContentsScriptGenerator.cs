
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Extensions;
using Modules.Devkit.Console;
using Modules.Devkit.Generators;

namespace Modules.TextData.Editor
{
    public sealed class ContentsScriptGenerator
    {
        //----- params -----

        private const string ContentsScriptTemplate =
@"
// Generated by ContentsScriptGenerator.cs

using System;
using System.Collections.Generic;

namespace Modules.TextData
{
    public partial class TextData
	{
        #SUMMARY#
        public enum #ENUMNAME#
        {
#ENUMS#
        }

        private readonly IReadOnlyDictionary<Enum, string> _#ENUMNAME#Table = new Dictionary<Enum, string>
        {
#ELEMENTS#
        };
    }
}
";

        private const string EnumElementTemplate = @"{0},";

        private const string SummaryTemplate = @"/// <summary> {0} </summary>";

        private const string TableElementTemplate = @"{{ {0}.{1}, ""{2}"" }},";

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(SheetData[] sheets, string scriptFolderPath, int textIndex)
        {
            var generatedScripts = new List<string>();

            foreach (var sheet in sheets)
            {
                var enums = new StringBuilder();
                var elements = new StringBuilder();

                var sheetRecords = sheet.records;

                for (var i = 0; i < sheetRecords.Count; ++i)
                {
                    var record = sheetRecords[i];

                    var enumName = ScriptGenerateUtility.RemoveInvalidChars(record.enumName);

                    if (string.IsNullOrEmpty(enumName)) { continue; }

                    var text = record.texts.ElementAtOrDefault(textIndex);

                    if (!string.IsNullOrEmpty(text))
                    {
                        // 改行を置き換え.
                        text = text.Replace("\r\n", "").Replace("\n", "");
                        // タグ文字を置き換え.
                        text = text.Replace("<", "&lt;").Replace(">", "&gt;");
                    }
                    else
                    {
                        text = string.Empty;
                    }

                    var summary = string.Format(SummaryTemplate, text);

                    enums.Append("\t\t\t").AppendLine(summary);
                    enums.Append("\t\t\t").AppendFormat(EnumElementTemplate, record.enumName);

                    elements.Append("\t\t\t").AppendFormat(TableElementTemplate, sheet.sheetName, record.enumName, record.guid);

                    // 最終行は改行しない.
                    if (i < sheetRecords.Count - 1)
                    {
                        enums.AppendLine();
                        enums.AppendLine();

                        elements.AppendLine();
                    }
                }

                var script = ContentsScriptTemplate;

                script = Regex.Replace(script, "#SUMMARY#", string.Format(SummaryTemplate, sheet.displayName));
                script = Regex.Replace(script, "#ENUMNAME#", ScriptGenerateUtility.RemoveInvalidChars(sheet.sheetName));
                script = Regex.Replace(script, "#ENUMS#", enums.ToString());

                script = Regex.Replace(script, "#ELEMENTS#", elements.ToString());

                script = script.FixLineEnd();

                var fileName = GetSctiptFileName(sheet);

                if (ScriptGenerateUtility.GenerateScript(scriptFolderPath, fileName, script))
                {
                    generatedScripts.Add(fileName);
                }
            }

            // 定義が消えたファイルを削除.
            DeleteUnusedFiles(generatedScripts.ToArray(), scriptFolderPath);
        }

        private static void DeleteUnusedFiles(string[] generatedScripts, string scriptFolderPath)
        {
            var exportFullPath = PathUtility.Combine(UnityPathUtility.GetProjectFolderPath(), scriptFolderPath);

            if (AssetDatabase.IsValidFolder(scriptFolderPath))
            {
                var files = Directory.GetFiles(exportFullPath, "*", SearchOption.TopDirectoryOnly);

                var deleteTargets = files
                    .Where(x => Path.GetFileName(x) != TextDataScriptGenerator.TextDataScriptFileName)
                    .Where(x => Path.GetFileName(x) != CategoryScriptGenerator.CategoryScriptFileName)
                    .Where(x => Path.GetExtension(x) != ".meta" && !generatedScripts.Contains(Path.GetFileName(x)))
                    .Select(x => UnityPathUtility.ConvertFullPathToAssetPath(x));

                foreach (var target in deleteTargets)
                {
                    if (Path.GetExtension(target) == ".cs")
                    {
                        AssetDatabase.DeleteAsset(target);
                        UnityConsole.Info("File Delete : {0}", target);
                    }
                }
            }
        }

        public static string GetSctiptFileName(SheetData sheet)
        {
            return $"{sheet.sheetName}.cs";
        }
    }
}
