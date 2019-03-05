
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

        public static byte[] Serialize(string csv, bool lz4Compress = false)
        {
            var textBytes = Encoding.UTF8.GetBytes(csv);

            var typeGenerator = GenerateSerializeType("CSVSerializeType", textBytes);

            using (var memoryStream = new MemoryStream(textBytes))
            {
                var bytes = new byte[0];

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

                        // レコード情報を生成した型情報でパース.
                        var data = csvReader.GetRecords(typeGenerator.Type).ToArray();

                        // Json化.
                        var json = JsonFx.Json.JsonWriter.Serialize(data);

                        // シリアライズ.
                        bytes = lz4Compress ?
                            LZ4MessagePackSerializer.FromJson(json) :
                            MessagePackSerializer.FromJson(json);
                    }
                }

                return bytes;
            }
        }

        private static TypeGenerator GenerateSerializeType(string className, byte[] textBytes)
        {
            TypeGenerator typeGenerator = null;

            using (var memoryStream = new MemoryStream(textBytes))
            {
                using (var streamReader = new StreamReader(memoryStream))
                {
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

                        csvReader.Read();

                        var properties = new Dictionary<string, Type>();

                        for (var i = 0; i < csvReader.CurrentRecord.Length; i++)
                        {
                            if (csvReader.FieldHeaders.Length <= i) { continue; }

                            var type = TypeUtility.GetTypeFromTypeName(csvReader.FieldHeaders[i]);
                            var name = csvReader.CurrentRecord.ElementAt(i);

                            if (type == null || string.IsNullOrEmpty(name)) { continue; }

                            properties.Add(name, type);
                        }

                        typeGenerator = new TypeGenerator(className, properties);
                    }
                }
            }

            return typeGenerator;
        }
    }
}
