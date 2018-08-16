﻿﻿
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Google.GData.Spreadsheets;
using UniRx;

namespace Modules.Devkit.Spreadsheet
{
    public class SheetEntity
    {
        //----- params -----

        public class Row
        {
            public int RowNumber { get; set; }
            public SortedDictionary<string, string> Values { get; set; }

            public override string ToString()
            {
                return RowNumber + " - " + string.Join(", ", Values.Select(x => x.Key + ":" + x.Value).ToArray());
            }
        }

        //----- field -----

        //----- property -----

        public string Title { get; private set; }
        public DateTime LastUpdateDate { get; private set; }
        public Row[] Rows { get; private set; }

        //----- method -----

        public SheetEntity(string title, DateTime lastUpdateDate, IEnumerable<CellEntry> source)
        {
            Title = title;

            // ミリ秒を除外する.
            var ticks = lastUpdateDate.Ticks - ( lastUpdateDate.Ticks % TimeSpan.TicksPerSecond );
            LastUpdateDate = new DateTime(ticks, lastUpdateDate.Kind).ToLocalTime();

            Rows = SplitRows(source).ToArray();
        }

        // Columnは空白があると省略される.
        private static IEnumerable<Row> SplitRows(IEnumerable<CellEntry> source)
        {
            // cell毎にD13とかで来るので分割して一行毎に変換.
            return source
                .Select(cell =>
                {
                    var match = Regex.Match(cell.Title.Text, "([A-Z]+)([0-9]+)");
                    var column = match.Groups[1].Value;
                    var row = int.Parse(match.Groups[2].Value);

                    return UniRx.Tuple.Create(cell, column, row);
                })
                .GroupBy(x => x.Item3)
                .OrderBy(x => x.Key)
                .Select(x =>
                {
                    var values = new SortedDictionary<string, string>();

                    foreach (var item in x)
                    {
                        values.Add(item.Item2, item.Item1.Value);
                    }

                    return new Row { RowNumber = x.Key, Values = values };
                });
        }

        /// <summary>
        /// 行列アドレス("A1")形式から値を取得.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public string GetValue(string address)
        {
            var match = Regex.Match(address, "([A-Z]+)([0-9]+)");
            var column = match.Groups[1].Value;
            var row = int.Parse(match.Groups[2].Value);

            var rowEntity = Rows.FirstOrDefault(x => x.RowNumber == row);

            if (rowEntity != null)
            {
                if(rowEntity.Values.ContainsKey(column))
                {
                    return rowEntity.Values[column];
                }
            }

            return null;
        }

        /// <summary>
        /// 行番号、列番号から値を取得.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public string GetValue(int column, int row)
        {
            // Excelは1開始なのでインデックスを+1する.
            var address = SpreadsheetUtility.ConvertColumnName(column+1) + (row+1).ToString();

            return GetValue(address);
        }
    }
}