
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Spreadsheet;

namespace Modules.LocalMaster
{
    public interface ILocalMasterAssetGenerator
    {
        Type MasterAssetType { get; }
        string MasterName { get; }

        void Generate(UnityEngine.Object asset, LocalMasterConfig config, SheetEntity spreadsheet);
    }

    public class LocalMasterAssetGenerator : ILocalMasterAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Type MasterAssetType { get; private set; }
        public string MasterName { get; private set; }

        //----- property -----

        //----- method -----

        public LocalMasterAssetGenerator(Type masterAssetType, string masterName)
        {
            this.MasterAssetType = masterAssetType;
            this.MasterName = masterName;
        }

        public void Generate(UnityEngine.Object asset, LocalMasterConfig config, SheetEntity spreadsheet)
        {
            var masterTable = asset as LocalMasterAsset;

            if (spreadsheet == null) { return; }

            var dataStartAddress = spreadsheet.GetValue(config.DataStartAddress);

            // 最新のシート情報で上書きする為クリア.
            masterTable.TableClear();

            var row = 0;
            var column = 0;

            //===== フィールド名を収集 =====

            row = SpreadsheetUtility.GetRow(dataStartAddress);
            column = SpreadsheetUtility.ColumnNameToNumber(dataStartAddress);

            var fieldDictionary = new Dictionary<int, string>();
            var fieldRow = spreadsheet.Rows.FirstOrDefault(x => x.RowNumber == row);

            // フィールド情報がない為終了.
            if(fieldRow == null) { return; }

            while (true)
            {
                var fieldName = fieldRow.Values
                    .Where(x => x.Key == SpreadsheetUtility.ConvertColumnName(column))
                    .Select(x => x.Value)
                    .FirstOrDefault();

                if (fieldName == null) { break; }

                fieldDictionary.Add(column, fieldName.Trim());

                column++;
            }

            //===== テーブルを収集 =====

            row = SpreadsheetUtility.GetRow(dataStartAddress) + 1;
            column = SpreadsheetUtility.ColumnNameToNumber(dataStartAddress);

            while (true)
            {
                var tableRow = spreadsheet.Rows.FirstOrDefault(x => x.RowNumber == row);

                if (tableRow == null) { break; }

                var masterData = new Dictionary<string, string>();

                foreach (var fieldInfo in fieldDictionary)
                {
                    var value = tableRow.Values
                        .Where(x => x.Key == SpreadsheetUtility.ConvertColumnName(fieldInfo.Key))
                        .Select(x => x.Value)
                        .FirstOrDefault();

                    masterData.Add(fieldInfo.Value, value);
                }

                masterTable.TableInsert(masterData);

                row++;
            }
        }
    }
}