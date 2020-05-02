
using System.IO;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Modules.GameText.Editor
{
    public static class FileSystem
    {
        public enum Format
        {
            Yaml,
            Json,
        }

        public static void WriteFile<T>(string filePath, T value, Format format)
        {
            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var writer = new StreamWriter(file, new UTF8Encoding(false)))
                {
                    switch (format)
                    {
                        case Format.Json:
                            {
                                using (var jsonTextWriter = new JsonTextWriter(writer))
                                {
                                    jsonTextWriter.Formatting = Formatting.Indented;

                                    var jsonSerializer = new JsonSerializer();

                                    jsonSerializer.Serialize(jsonTextWriter, value);
                                }
                            }
                            break;

                        case Format.Yaml:
                            {
                                var yamlSerializer = new SerializerBuilder().Build();

                                yamlSerializer.Serialize(writer, value);
                            }
                            break;
                    }
                }
            }
        }

        public static T LoadFile<T>(string filePath, Format format) where T : class
        {
            if (!File.Exists(filePath)) { return null; }

            T result = null;

            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(file, new UTF8Encoding(false)))
                {
                    switch (format)
                    {
                        case Format.Json:
                            {
                                using (var jsonTextReader = new JsonTextReader(reader))
                                {
                                    var jsonSerializer = new JsonSerializer();

                                    result = jsonSerializer.Deserialize<T>(jsonTextReader);
                                }
                            }
                            break;

                        case Format.Yaml:
                            {
                                var contents = reader.ReadToEnd();

                                var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                                
                                result = deserializer.Deserialize<T>(contents);
                            }
                            break;
                    }
                }
            }

            return result;
        }
    }
}
