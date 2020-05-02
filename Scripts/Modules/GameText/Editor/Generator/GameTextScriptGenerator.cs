﻿
using System.Text;
using System.Text.RegularExpressions;
using Extensions;
using Modules.Devkit.Generators;

namespace Modules.GameText.Editor
{
    public sealed class GameTextScriptGenerator
    {
        //----- params -----

        public const string GameTextScriptFileName = @"GameText.definition.cs";

        private const string GameTextScriptTemplate =
@"

// Generated by GameTextScriptGenerator.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.GameText
{
    public partial class GameText
    {
        //----- params -----
    
        private sealed class CategoryDefinition
        {
            public CategoryType Category { get; private set; }

            public Type EnumType { get; private set; }

            public IReadOnlyDictionary<Enum, string> Table { get; private set; }

            public string Guid { get; private set; }

            public CategoryDefinition(CategoryType category, Type enumType, IReadOnlyDictionary<Enum, string> table, string guid)
            {
                Category = category;
                EnumType = enumType;
                Table = table;
                Guid = guid;
            }
        }

        //----- field -----

        private IReadOnlyList<CategoryDefinition> categoryDefinition = null;

        //----- property -----

        //----- method -----

        protected override void BuildGenerateContents()
        {
            categoryDefinition = new CategoryDefinition[]
            {
#CATEGORY_DEFINITION_ITEMS#
            };
        }

        public override Type GetCategoriesType()
        {
            return typeof(CategoryType);
        }

        public override string FindCategoryGuid(Enum categoryType)
        {
            CategoryDefinition info = null;

            if (categoryType is CategoryType)
            {
                info = categoryDefinition.FirstOrDefault(x => x.Category == (CategoryType)categoryType);
            }
            
            return info == null ? null : info.Guid;
        }

        public override Type FindCategoryEnumType(string categoryGuid)
        {
            var info = categoryDefinition.FirstOrDefault(x => x.Guid == categoryGuid);

            if (info == null) { return null; }

            return info.EnumType;
        }

        public override Enum FindCategoryDefinitionEnum(string categoryGuid)
        {
            var info = categoryDefinition.FirstOrDefault(x => x.Guid == categoryGuid);

            if (info == null) { return null; }

            return info.Category;
        }

        public override IReadOnlyDictionary<Enum, string> FindCategoryTexts(string categoryGuid)
        {
            var info = categoryDefinition.FirstOrDefault(x => x.Guid == categoryGuid);

            if (info == null) { return null; }

            return info.Table;
        }

        public override string FindTextGuid(Enum textType)
        {
            var table = categoryDefinition.FirstOrDefault(x => x.EnumType == textType.GetType());

            if (table == null) { return null; }

            var item = table.Table.FirstOrDefault(x => x.Key == textType);

            if (item.Equals(default(KeyValuePair<Enum, string>))) { return null; }

            return item.Value;
        }

#GETTEXT_METHODS#
    }
}
";

        private const string CategoryDefinitionTemplate = @"new CategoryDefinition(CategoryType.{0}, typeof(GameText.{0}), _{0}Table, ""{1}""),";

        private const string GetMethodTemplate =
@"
		public static string Get(GameText.{0} textType)
		{{
		    return GameText.Instance.Cache.GetValueOrDefault(GameText.Instance.FindTextGuid(textType));
		}}
";

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(SheetData[] sheets, GameTextConfig config)
        {
            var exportPath = config.EnumScriptFolderPath;

            var categorys = new StringBuilder();
            var getMethods = new StringBuilder();

            for (var i = 0; i < sheets.Length; i++)
            {
                var sheet = sheets[i];

                categorys.Append("\t\t\t\t").AppendFormat(CategoryDefinitionTemplate, sheet.sheetName, sheet.guid);

                getMethods.Append("\t\t").AppendFormat(GetMethodTemplate, sheet.sheetName);

                // 最終行は改行しない.
                if (i < sheets.Length - 1)
                {
                    categorys.AppendLine();

                    getMethods.AppendLine().AppendLine();
                }
            }

            var script = GameTextScriptTemplate;

            script = Regex.Replace(script, "#CATEGORY_DEFINITION_ITEMS#", categorys.ToString());
            script = Regex.Replace(script, "#GETTEXT_METHODS#", getMethods.ToString());

            script = script.FixLineEnd();
            
            ScriptGenerateUtility.GenerateScript(exportPath, GameTextScriptFileName, script);            
        }
    }
}
