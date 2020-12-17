﻿
using System;
using MessagePack;
using MessagePack.Formatters;

namespace Modules.MessagePack
{
    public class DateTimeResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DateTimeResolver();

        DateTimeResolver() { }

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
                    return new DateTimeFormatter();
                }
                else if (t == typeof(DateTime?))
                {
                    return new StaticNullableFormatter<DateTime>(new DateTimeFormatter());
                }

                return null;
            }
        }
    }

    public class DateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public void Serialize(ref MessagePackWriter writer, DateTime value, MessagePackSerializerOptions options)
        {
            var dateData = value.ToBinary();

            writer.Write(dateData);
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
                var dateData = reader.ReadInt64();

                return DateTime.FromBinary(dateData);
            }           
        }
    }
}
