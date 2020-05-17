
using System.IO;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Modules.GameText.Editor
{
    public static class FileLoader
    {
        public enum Format
        {
            Yaml,
            Json,
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
                                    var jsonSerializer = new JsonSerializer()
                                    {
                                        NullValueHandling = NullValueHandling.Ignore,
                                    };

                                    result = jsonSerializer.Deserialize<T>(jsonTextReader);
                                }
                            }
                            break;

                        case Format.Yaml:
                            {
                                var contents = reader.ReadToEnd();

                                var builder = new DeserializerBuilder();

                                builder.IgnoreUnmatchedProperties();

                                var yamlDeserializer = builder.Build();

                                result = yamlDeserializer.Deserialize<T>(contents);
                            }
                            break;
                    }
                }
            }

            return result;
        }
    }
}
