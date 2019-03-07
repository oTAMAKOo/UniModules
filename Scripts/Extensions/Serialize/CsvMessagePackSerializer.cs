
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;
using MessagePack;

namespace Extensions.Serialize
{
    public class CsvMessagePackSerializer
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static byte[] Serialize(string csv, string[] exportMarks, bool lz4Compress = false)
        {
            var textBytes = Encoding.UTF8.GetBytes(csv);

            var exportFlags = exportMarks.Any() ? GetExportFlags(textBytes, exportMarks) : null;

            var typeGenerator = GenerateSerializeType(textBytes, exportFlags);

            var bytes = BuildMessagePack(textBytes, typeGenerator, lz4Compress);

            return bytes;
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

        private static TypeGenerator GenerateSerializeType(byte[] textBytes, bool[] exportFlags)
        {
            TypeGenerator typeGenerator = null;

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

                            var type = TypeUtility.GetTypeFromSystemTypeName(csvReader.FieldHeaders[i]);
                            var name = csvReader.CurrentRecord.ElementAt(i);

                            if (type == null || string.IsNullOrEmpty(name)) { continue; }

                            properties.Add(name, type);
                        }

                        typeGenerator = new TypeGenerator("CSVSerializeType", properties);
                    }
                }
            }

            return typeGenerator;
        }

        public static byte[] BuildMessagePack(byte[] textBytes, TypeGenerator typeGenerator, bool lz4Compress)
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
                            var instance = CreateInstanceFromCsvRecord(typeGenerator, csvReader.FieldHeaders, csvReader.CurrentRecord);

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

        private static object CreateInstanceFromCsvRecord(TypeGenerator typeGenerator, string[] headers, string[] values)
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

                    var value = type.GetDefaultValue();

                    if (!string.IsNullOrEmpty(values[i]))
                    {
                        if (type.IsArray)
                        {
                            var elementType = type.GetElementType();

                            var str = values[i];

                            str = str.Trim(new char[] { ' ', '\n', '\t' });

                            var list = new List<object>();

                            // 複数要素のある場合.
                            if (str.StartsWith("[") && str.EndsWith("]"))
                            {
                                // 文字列要素でない場合は余計な文字を削除.
                                if (elementType != typeof(string))
                                {
                                    str = str.Replace(" ", string.Empty);
                                    str = str.Replace("\n", string.Empty);
                                    str = str.Replace("\t", string.Empty);
                                }

                                // 「[]」を外す.
                                str = str.Substring(1, str.Length - 1).Substring(0, str.Length - 2);

                                // 「,」区切りで配列化.
                                var elements = str.Split(',').Where(x => !string.IsNullOrEmpty(x));

                                foreach (var element in elements)
                                {
                                    list.Add(Convert.ChangeType(element, elementType));
                                }
                            }
                            // 「[]」で囲まれてない場合は1つしか要素がない配列に変換.
                            else
                            {
                                list.Add(Convert.ChangeType(str, elementType));
                            }

                            var array = Array.CreateInstance(elementType, list.Count);
                            Array.Copy(list.ToArray(), array, list.Count);

                            value = array;
                        }
                        else
                        {
                            value = Convert.ChangeType(values[i], type);
                        }
                    }

                    value = value != null ? Convert.ChangeType(value, type) : null;

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

        private static object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
        }
    }
}
