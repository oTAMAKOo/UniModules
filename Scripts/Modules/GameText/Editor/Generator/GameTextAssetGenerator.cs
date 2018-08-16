﻿﻿
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.Spreadsheet;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public class GameTextAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Build(GameTextAsset asset, SheetEntity[] spreadsheets, GameTextConfig config, int textColumn)
        {
            var sheetDefinitionRow = config.SheetDefinitionRow.GetValueOrDefault();
            var sheetIdColumn = config.SheetIdColumn.GetValueOrDefault();

            var startRow = config.DefinitionStartRow.GetValueOrDefault();
            var idColumn = config.IdColumn.GetValueOrDefault();
            var ignoreSheets = config.IgnoreSheets;

            var gameTextDictionarys = new List<GameTextDictionary>();

            var targetSheets = spreadsheets.Where(x => !ignoreSheets.Contains(x.Title)).ToArray();

            foreach (var spreadsheet in targetSheets)
            {
                var seatId = 0;
                var seatIdText  = spreadsheet.GetValue(sheetIdColumn, sheetDefinitionRow);

                if (!int.TryParse(seatIdText, out seatId)){ continue; }

                var gameTextDictionary = new GameTextDictionary();

                gameTextDictionary.SheetName = spreadsheet.Title;
                gameTextDictionary.SheetId = seatId;

                for (var i = startRow; i < spreadsheet.Rows.Length; ++i)
                {
                    var id = 0;
                    var idText = spreadsheet.GetValue(idColumn, i);
                    var text = spreadsheet.GetValue(textColumn, i);

                    // IDが数値に変換出来ない値の場合は追加しない.
                    if (!int.TryParse(idText, out id)){ continue; }

                    gameTextDictionary.Add(id, text);
                }

                gameTextDictionarys.Add(gameTextDictionary);
            }

            asset.contents = gameTextDictionarys.ToArray();
        }
    }
}