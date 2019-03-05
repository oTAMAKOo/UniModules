
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using CsvHelper;
using ExcelDataReader;

namespace Extensions
{
    public static class ExcelUtility
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary>
        /// Excelファイルを読み込み.
        /// </summary>
        public static DataSet LoadExcelData(string filePath)
        {
            DataSet result = null;

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var excelReader = ExcelReaderFactory.CreateReader(stream))
                {
                    result = excelReader.AsDataSet();
                }
            }

            return result;
        }

        /// <summary>
        /// ExcelファイルデータからCSVを作成.
        /// </summary>
        public static void GenerateCsvFromExcel(string exportPath, DataSet excelData, string tableName)
        {
            using (var textWriter = File.CreateText(exportPath))
            {
                using (var csvWriter = new CsvWriter(textWriter))
                {
                    // 文字コード.
                    csvWriter.Configuration.Encoding = Encoding.UTF8;
                    // コメントを有効.
                    csvWriter.Configuration.AllowComments = true;
                    // 先頭が'#'の行はコメント扱い.
                    csvWriter.Configuration.Comment = '#';

                    var table = excelData.Tables[tableName];

                    foreach (DataRow row in table.Rows)
                    {
                        // 末尾の空セルを削除.

                        var delCount = 0;
                        var list = row.ItemArray.ToList();

                        for (var i = list.Count - 1 ; 0 < i ; i--)
                        {
                            var str = list[i].ToString();

                            if (!string.IsNullOrEmpty(str)) { break; }

                            delCount++;
                        }

                        if (0 < delCount)
                        {
                            list.RemoveRange(list.Count - delCount, delCount);
                        }

                        // 書き込み.

                        for (var i = 0; i < list.Count; i++)
                        {
                            // Null文字を空データに変換.

                            var value = list[i];

                            if(value.ToString().ToLower() == "null")
                            {
                                value = string.Empty;
                            }

                            csvWriter.WriteField(value);
                        }

                        csvWriter.NextRecord();
                    }
                }
            }
        }

        /// <summary>
        /// CSVファイルを読み込み.
        /// </summary>
        public static string LoadCsv(string filePath, int headerRow = 0)
        {
            var builder = new StringBuilder();

            using (var streamReader = new StreamReader(filePath, Encoding.UTF8))
            {
                using (var csvParser = new CsvParser(streamReader))
                {
                    // 文字コード.
                    csvParser.Configuration.Encoding = Encoding.UTF8;

                    var index = 0;

                    while (csvParser.Read() != null)
                    {
                        // ヘッダー行までの行をスキップ.
                        if (headerRow <= index)
                        {
                            builder.AppendLine(csvParser.RawRecord);
                        }

                        index++;
                    }
                }
            }

            return builder.ToString();
        }
    }
}
