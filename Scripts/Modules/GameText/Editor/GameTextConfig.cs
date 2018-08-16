﻿﻿
using UnityEngine;
using UnityEditor;
using Extensions.Serialize;
using Modules.Devkit.ScriptableObjects;

namespace Modules.GameText.Editor
{
	public class GameTextConfig : ReloadableScriptableObject<GameTextConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private UnityEngine.Object tableScriptFolder = null;
        [SerializeField]
        private UnityEngine.Object enumScriptFolder = null;
        [SerializeField]
        private UnityEngine.Object scriptableObjectFolder = null;
        [SerializeField]
        private string spreadsheetId = string.Empty;
        [SerializeField]
        private string[] ignoreSheets = new string[0];

        [SerializeField]
        private IntNullable sheetDefinitionRow = null;
        [SerializeField]
        private IntNullable sheetIdColumn = null;
        [SerializeField]
        private IntNullable sheetNameColumn = null;

        [SerializeField]
        private IntNullable definitionStartRow = null;
        [SerializeField]
        private IntNullable idColumn = null;
        [SerializeField]
        private IntNullable enumColumn = null;

        //----- property -----

        public string TableScriptFolderPath { get { return AssetDatabase.GetAssetPath(tableScriptFolder); } }
        public string EnumScriptFolderPath { get { return AssetDatabase.GetAssetPath(enumScriptFolder); } }
        public string ScriptableObjectFolderPath { get { return AssetDatabase.GetAssetPath(scriptableObjectFolder); } }
        public string SpreadsheetId { get { return spreadsheetId; } }
        public string[] IgnoreSheets { get { return ignoreSheets; } }

        public IntNullable SheetDefinitionRow { get { return sheetDefinitionRow; } }
        public IntNullable SheetIdColumn { get { return sheetIdColumn; } }
        public IntNullable SheetNameColumn { get { return sheetNameColumn; } }

        public IntNullable DefinitionStartRow { get { return definitionStartRow; } }
        public IntNullable IdColumn { get { return idColumn; } }
        public IntNullable EnumColumn { get { return enumColumn; } }

        //----- method -----
    }
}