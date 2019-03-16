using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;

namespace Extensions.Serialize
{
    public class CsvSerializeDataGenerator
    {
        public static object[] BuildSerializeData(string csv, string[] exportMarks)
        {
            var textBytes = Encoding.UTF8.GetBytes(csv);
            
            var exportFlags = exportMarks.Any() ? GetExportFlags(textBytes, exportMarks) : null;

            var typeGenerator = GenerateCsvTypeGenerator(textBytes, exportFlags);

            var values = BuildClassValues(typeGenerator, textBytes);

            return values;
        }

        private static TypeGenerator GenerateCsvTypeGenerator(byte[] textBytes, bool[] exportFlags)
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

                        return new TypeGenerator("CSVSerializeType", properties);
                    }
                }
            }
        }

        private static bool[] GetExportFlags(byte[] textBytes, string[] exportMarks)
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

        private static object[] BuildClassValues(TypeGenerator typeGenerator, byte[] textBytes)
        {
            var list = new List<object>();

            using (var memoryStream = new MemoryStream(textBytes))
            {
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
                        while (csvReader.Read())
                        {
                            var header = csvReader.FieldHeaders;
                            var record = csvReader.CurrentRecord;

                            var instance = CreateInstanceFromCsvRecord(typeGenerator, header, record);

                            list.Add(instance);
                        }
                    }
                }

                return list.ToArray();
            }
        }

        private static object CreateInstanceFromCsvRecord(TypeGenerator typeGenerator, string[] headers, string[] values)
        {
            var instance = typeGenerator.NewInstance();

            for (var i = 0; i < values.Length; i++)
            {
                var name = headers[i].Trim();

                try
                {
                    var property = typeGenerator.Type.GetProperty(name);

                    var value = ParseValueObject(values[i], property.PropertyType);

                    TypeGenerator.SetProperty(instance, name, value, property.PropertyType);
                }
                catch
                {
                    Console.WriteLine(string.Format("CsvRecord error. [ERROR:{0}]\n{1}\n", name, string.Join(", ", values)));
                    throw;
                }
            }

            return instance;
        }

        private static object ParseValueObject(string valueText, Type valueType)
        {
            var value = valueType.GetDefaultValue();

            // Null許容型.
            var underlyingType = Nullable.GetUnderlyingType(valueType);

            if (underlyingType != null)
            {
                valueType = underlyingType;
            }

            // 空文字列ならデフォルト値.
            if (string.IsNullOrEmpty(valueText))
            {
                // Null許容型.
                if (underlyingType != null) { return null; }

                // 配列.
                if (valueType.IsArray)
                {
                    var elementType = valueType.GetElementType();

                    return Array.CreateInstance(elementType, 0);
                }

                return value;
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
                    var elements = arrayText.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToArray();

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

        private static object ParseValue(string valueText, Type valueType)
        {
            if (valueType != typeof(string))
            {
                valueText = valueText.Trim(' ', '　', '\n', '\t');
            }

            return Convert.ChangeType(valueText, valueType, CultureInfo.InvariantCulture);
        }
    }
}
