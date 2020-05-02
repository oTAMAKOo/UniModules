﻿
using System.Text;
using System.Text.RegularExpressions;
using Extensions;
using Modules.Devkit.Generators;

namespace Modules.GameText.Editor
{
    public sealed class CategoryScriptGenerator
    {
        //----- params -----

        public const string CategoryScriptFileName = @"GameText.category.cs";

        private const string CategoryScriptTemplate =
@"

// Generated by CategoryScriptGenerator.cs

using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.GameText
{
    public enum CategoryType
    {
#ENUMS#
    }

    public partial class GameText
    {
        public sealed class CategoryInfo
        {
            public string Guid { get; private set; }

            public CategoryType Category { get; private set; }

            public Type EnumType { get; private set; }

            public CategoryInfo(CategoryType category, Type enumType, string guid)
            {
                Guid = guid;
                Category = category;
                EnumType = enumType;
            }
        }

        private readonly IReadOnlyList<CategoryInfo> CategoryTable = new List<CategoryInfo>
        {
#ELEMENTS#
        };
    }
}
";
        private const string CategoryLabelTemplate = @"[Label(""{0}"")]";

        private const string EnumContentsTemplate = @"{0},";

        private const string TableElementTemplate = @"new CategoryInfo(CategoryType.{0}, typeof({0}), ""{1}""),";

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(SheetData[] sheets, GameTextConfig config)
        {
            var exportPath = config.TableScriptFolderPath;

            var enums = new StringBuilder();
            var contents = new StringBuilder();

            for (var i = 0; i < sheets.Length; ++i)
            {
                var sheet = sheets[i];

                enums.Append("\t\t").AppendFormat(CategoryLabelTemplate, sheet.displayName).AppendLine();

                enums.Append("\t\t").AppendFormat(EnumContentsTemplate, sheet.sheetName);

                contents.Append("\t\t\t").AppendFormat(TableElementTemplate, sheet.sheetName, sheet.guid);

                // 最終行は改行しない.
                if (i < sheets.Length - 1)
                {
                    enums.AppendLine();
                    enums.AppendLine();

                    contents.AppendLine();
                }
            }

            var script = CategoryScriptTemplate;

            script = Regex.Replace(script, "#ENUMS#", enums.ToString());
            script = Regex.Replace(script, "#ELEMENTS#", contents.ToString());

            script = script.FixLineEnd();

            ScriptGenerateUtility.GenerateScript(exportPath, CategoryScriptFileName, script);
        }
    }
}
