
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Extensions
{
    public static class SerializationFileUtility
    {
        public enum Format
        {
            Yaml,
            Json,
        }

        public static void WriteFile<T>(string filePath, T value, Format format)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

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
                                    var jsonSerializer = new JsonSerializer()
                                    {
                                        Formatting = Formatting.Indented,
                                        NullValueHandling = NullValueHandling.Ignore,
                                    };

                                    jsonSerializer.Serialize(jsonTextWriter, value);
                                }
                            }
                            break;

                        case Format.Yaml:
                            {
                                var yamlSerializer = new SerializerBuilder()
                                    .WithEventEmitter(nextEmitter => new NullStringsAsEmptyEventEmitter(nextEmitter))
                                    .Build();

                                yamlSerializer.Serialize(writer, value);
                            }
                            break;
                    }
                }
            }
        }
         
        public static T LoadFile<T>(string filePath, Format format) where T : class 
        {
            return LoadFile(filePath, typeof(T), format) as T;
        }

        public static object LoadFile(string filePath, Type type, Format format)
        {
            if (!File.Exists(filePath)) { return null; }

            object result = null;

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
                                        Formatting = Formatting.Indented,
                                        NullValueHandling = NullValueHandling.Ignore,
                                    };

                                    result = jsonSerializer.Deserialize(jsonTextReader, type);
                                }
                            }
                            break;

                        case Format.Yaml:
                            {
                                var contents = reader.ReadToEnd();

                                var builder = new DeserializerBuilder();

                                builder.IgnoreUnmatchedProperties();

                                var yamlDeserializer = builder.Build();
                                
                                result = yamlDeserializer.Deserialize(contents, type);
                            }
                            break;
                    }
                }
            }

            return result;
        }

        private sealed class NullStringsAsEmptyEventEmitter : ChainedEventEmitter
        {
            public NullStringsAsEmptyEventEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

            public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
            {
                if (eventInfo.Source.Type == typeof(string) && eventInfo.Source.Value == null)
                {
                    emitter.Emit(new Scalar(string.Empty));
                }
                else
                {
                    base.Emit(eventInfo, emitter);
                }
            }
        }
    }
}
