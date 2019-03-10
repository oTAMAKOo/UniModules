
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using MessagePack;

namespace Extensions.Serialize
{
    public sealed class CsvMessagePackSerializer
    {
        //----- params -----

        //----- field -----

        private TypeGenerator typeGenerator = null;

        //----- property -----

        //----- method -----

        public byte[] Serialize(string csv, string[] exportMarks, bool lz4Compress = false)
        {
            var textBytes = Encoding.UTF8.GetBytes(csv);

            var exportFlags = exportMarks.Any() ? GetExportFlags(textBytes, exportMarks) : null;

            GenerateSerializeType(textBytes, exportFlags);

            var bytes = BuildMessagePack(textBytes, lz4Compress);

            return bytes;
        }

        private bool[] GetExportFlags(byte[] textBytes, string[] exportMarks)
        {
            using (var memoryStream = new MemoryStream(textBytes))
            {
                using (var streamReader = new StreamReader(memoryStream))
                {
                    using (var csvReader = new CsvReader(streamReader))
                    {
                        // 文字コード.
                        csvReader.Configuration.Encoding = Encoding.UTF8;
                        // ヘッダーなし.
                        csvReader.Configuration.HasHeaderRecord = false;

                        // 読み込み.
                        csvReader.Read();

                        var exportFlags = new List<bool>();

                        for (var i = 0; i < csvReader.CurrentRecord.Length; i++)
                        {
                            var str = csvReader.CurrentRecord.ElementAt(i);

                            exportFlags.Add(exportMarks.Contains(str));
                        }

                        return exportFlags.ToArray();
                    }
                }
            }
        }

        private void GenerateSerializeType(byte[] textBytes, bool[] exportFlags)
        {
            using (var memoryStream = new MemoryStream(textBytes))
            {
                using (var streamReader = new StreamReader(memoryStream))
                {
                    streamReader.ReadLine();

                    using (var csvReader = new CsvReader(streamReader))
                    {
                        // 文字コード.
                        csvReader.Configuration.Encoding = Encoding.UTF8;
                        // コメントを有効.
                        csvReader.Configuration.AllowComments = true;
                        // 先頭が'#'の行はコメント扱い.
                        csvReader.Configuration.Comment = '#';
                        // ヘッダーの空白を削除.
                        csvReader.Configuration.IgnoreHeaderWhiteSpace = true;

                        // 読み込み.
                        csvReader.Read();

                        var properties = new Dictionary<string, Type>();

                        for (var i = 0; i < csvReader.CurrentRecord.Length; i++)
                        {
                            // 出力フラグがないので出力対象外.
                            if (exportFlags != null && i < exportFlags.Length)
                            {
                                if (!exportFlags[i]) { continue; }
                            }

                            if (csvReader.FieldHeaders.Length <= i) { continue; }

                            var typeName = csvReader.FieldHeaders[i];

                            var type = TypeUtility.GetTypeFromSystemTypeName(typeName);

                            var name = csvReader.CurrentRecord.ElementAt(i);

                            if (type == null || string.IsNullOrEmpty(name)) { continue; }

                            properties.Add(name, type);
                        }

                        typeGenerator = new TypeGenerator("CSVSerializeType", properties);
                    }
                }
            }
        }

        private byte[] BuildMessagePack(byte[] textBytes, bool lz4Compress)
        {
            using (var memoryStream = new MemoryStream(textBytes))
            {
                var bytes = new byte[0];

                using (var streamReader = new StreamReader(memoryStream))
                {
                    streamReader.ReadLine();
                    streamReader.ReadLine();

                    using (var csvReader = new CsvReader(streamReader))
                    {
                        // 文字コード.
                        csvReader.Configuration.Encoding = Encoding.UTF8;
                        // コメントを有効.
                        csvReader.Configuration.AllowComments = true;
                        // 先頭が'#'の行はコメント扱い.
                        csvReader.Configuration.Comment = '#';
                        // ヘッダーの空白を削除.
                        csvReader.Configuration.IgnoreHeaderWhiteSpace = true;

                        // レコード情報を生成した型情報でパース.

                        var data = new List<object>();

                        while (csvReader.Read())
                        {
                            var instance = CreateInstanceFromCsvRecord(csvReader.FieldHeaders, csvReader.CurrentRecord);
                            
                            data.Add(instance);
                        }

                        // Json化.
                        var json = JsonFx.Json.JsonWriter.Serialize(data.ToArray());

                        // シリアライズ.
                        bytes = lz4Compress ? LZ4MessagePackSerializer.FromJson(json) : MessagePackSerializer.FromJson(json);
                    }
                }

                return bytes;
            }
        }

        private object CreateInstanceFromCsvRecord(string[] headers, string[] values)
        {
            var instance = typeGenerator.NewInstance();

            for (var i = 0; i < values.Length; i++)
            {
                var name = headers[i].Trim();

                try
                {
                    // リフレクションでプロパティの型取得.
                    var property = typeGenerator.Type.GetProperty(name);

                    var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    var value = ParseValueObject(values[i], type);

                    TypeGenerator.SetProperty(instance, name, value, type);
                }
                catch
                {
                    Console.WriteLine(string.Format("CsvRecord error. [ERROR: {0}]\n{1}\n", name, string.Join(", ", values)));
                    throw;
                }
            }

            return instance;
        }

        private object ParseValueObject(string valueText, Type valueType)
        {
            var value = valueType.GetDefaultValue();

            // 空文字列ならデフォルト値.
            if (string.IsNullOrEmpty(valueText))
            {
                return valueType.GetDefaultValue();
            }

            // 配列.
            if (valueType.IsArray)
            {
                var list = new List<object>();

                var elementType = valueType.GetElementType();

                var arrayText = valueText;

                var start = arrayText.IndexOf("[", StringComparison.Ordinal);
                var end = arrayText.LastIndexOf("]", StringComparison.Ordinal);

                // 複数要素のある場合.
                if (start != -1 && end != -1 && start < end)
                {
                    // 「[]」を外す.
                    arrayText = arrayText.Substring(start + 1, end - start - 1);

                    // 「,」区切りで配列化.
                    var elements = arrayText.Split(',').ToArray();

                    foreach (var element in elements)
                    {
                        list.Add(ParseValue(element, elementType));
                    }
                }
                // 「[]」で囲まれてない場合は1つしか要素がない配列に変換.
                else
                {
                    list.Add(ParseValue(valueText, elementType));
                }

                var array = Array.CreateInstance(elementType, list.Count);

                Array.Copy(list.ToArray(), array, list.Count);

                value = array;
            }
            // 単一要素.
            else
            {
                value = ParseValue(valueText, valueType);
            }

            return value;
        }

        private object ParseValue(string valueText, Type valueType)
        {
            if(valueType != typeof(string))
            {
                valueText = valueText.Trim(' ', '　', '\n', '\t');
            }
            
            return Convert.ChangeType(valueText, valueType, CultureInfo.InvariantCulture);
        }
    }
}
