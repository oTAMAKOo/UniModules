﻿
using System;
using MessagePack;
using MessagePack.Formatters;

namespace Modules.MessagePack
{
    public class DateTimeResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DateTimeResolver();

        private DateTimeResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IMessagePackFormatter<T>)GetFormatterHelper(typeof(T));
            }

            private static object GetFormatterHelper(Type t)
            {
                if (t == typeof(DateTime))
                {
                    return DateTimeFormatter.Instance;
                }
                else if (t == typeof(DateTime?))
                {
                    return new StaticNullableFormatter<DateTime>(DateTimeFormatter.Instance);
                }

                return null;
            }
        }
    }

    public class DateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public static readonly DateTimeFormatter Instance = new DateTimeFormatter();

        public void Serialize(ref MessagePackWriter writer, DateTime value, MessagePackSerializerOptions options)
        {
            NativeDateTimeFormatter.Instance.Serialize(ref writer, value, options);
        }

        public DateTime Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.NextMessagePackType == MessagePackType.String)
            {
                var str = reader.ReadString();

                return DateTime.Parse(str);
            }
            else
            {
                return NativeDateTimeFormatter.Instance.Deserialize(ref reader, options);
            }           
        }
    }
}
