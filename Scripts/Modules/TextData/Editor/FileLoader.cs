
using System.IO;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Modules.TextData.Editor
{
    public static class FileLoader
    {
        public enum Format
        {
            Yaml,
            Json,
        }

        /// <summary> Json拡張子 </summary>
        private const string JsonFileExtension = ".json";

        /// <summary> Yaml拡張子 </summary>
        private const string YamlFileExtension = ".yaml";

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
                                        MissingMemberHandling = MissingMemberHandling.Ignore,
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

        public static string GetFileExtension(Format fileFormat)
        {
            var extension = string.Empty;

            switch (fileFormat)
            {
                case Format.Yaml:
                    extension = YamlFileExtension;
                    break;

                case Format.Json:
                    extension = JsonFileExtension;
                    break;
            }

            return extension;
        }
    }
}
