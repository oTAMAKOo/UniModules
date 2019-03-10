
using System;
using MessagePack;
using MessagePack.Formatters;

namespace Modules.MessagePack
{
    public class DateTimeResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new DateTimeResolver();

        DateTimeResolver(){}
        
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = typeof(T) == typeof(DateTime) ? (IMessagePackFormatter<T>)new DurableDateTimeFormatter() : null;
            }
        }
    }

    public class DurableDateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public int Serialize(ref byte[] bytes, int offset, DateTime value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteDateTime(ref bytes, offset, value);
        }

        public DateTime Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.GetMessagePackType(bytes, offset) == MessagePackType.String)
            {
                var str = MessagePackBinary.ReadString(bytes, offset, out readSize);
                return DateTime.Parse(str);
            }
            else
            {
                return MessagePackBinary.ReadDateTime(bytes, offset, out readSize);
            }
        }
    }
}
