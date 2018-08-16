﻿﻿
using System;
using System.Text.RegularExpressions;

namespace Modules.Devkit.Spreadsheet
{
    public static class SpreadsheetUtility
    {
        /// <summary>
        /// インデックスからカラム名に変換.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string ConvertColumnName(int index)
        {
            var str = string.Empty;

            do
            {
                str = Convert.ToChar(0x40 + index % 26) + str;
            }
            while ((index = index / 26 - 1) != -1);

            return str;
        }

        /// <summary>
        /// セル番地から行名を取得.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string GetColumn(string address)
        {
            var match = AddressMatch(address);
            return match.Groups[1].Value;
        }

        /// <summary>
        /// セル番地から行番号を取得.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static int ColumnNameToNumber(string address)
        {
            var columnName = GetColumn(address);

            columnName = columnName.ToUpperInvariant();

            var result = 0;

            for (int i = 0; i < columnName.Length; i++)
            {
                result *= 26;
                result += (columnName[i] - 'A' + 1);
            }

            return result;
        }

        /// <summary>
        /// セル番地から行番号を取得.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static int GetRow(string address)
        {
            var match = AddressMatch(address);
            return int.Parse(match.Groups[2].Value);
        }

        private static Match AddressMatch(string address)
        {
            return Regex.Match(address, "([A-Z]+)([0-9]+)");
        }
    }
}