
using System;
using MessagePack;
using MessagePack.Formatters;

namespace Modules.MessagePack
{
    public sealed class StringDateTimeResolver : IFormatterResolver
    {
        public static readonly StringDateTimeResolver Instance;

        static StringDateTimeResolver()
        {
            Instance = new StringDateTimeResolver();
        }

        private StringDateTimeResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)StringDateTimeResolverFormatterHelper.GetFormatter(typeof(T));
            }
        }

        private static class StringDateTimeResolverFormatterHelper
        {
            internal static object GetFormatter(Type t)
            {
                if (t == typeof(DateTime))
                {
                    return StringDateTimeFormatter.Instance;
                }
                
                if (t == typeof(DateTime?))
                {
                    return new StaticNullableFormatter<DateTime>(StringDateTimeFormatter.Instance);
                }

                return null;
            }
        }
    }

    public sealed class StringDateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public static readonly StringDateTimeFormatter Instance = new();

        private const string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        public void Serialize(ref MessagePackWriter writer, DateTime value, MessagePackSerializerOptions options)
        {
            var dateData = value.ToString(DateTimeFormat);

            writer.Write(dateData);
        }

        public DateTime Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.NextMessagePackType == MessagePackType.String)
            {
                var str = reader.ReadString();

                return DateTime.Parse(str);
            }
            
            return NativeDateTimeFormatter.Instance.Deserialize(ref reader, options);
        }
    }
}
