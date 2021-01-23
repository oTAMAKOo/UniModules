
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace Extensions
{
    /// <summary>
    /// object型の拡張メソッドを管理するクラス.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary> オブジェクトを複製  </summary>
        public static T DeepCopy<T>(this T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                var binaryFormatter = new BinaryFormatter();

                binaryFormatter.Serialize(memoryStream, obj);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }

        /// <summary> Json文字列に変換  </summary>
        public static string ToJson<T>(this T obj, bool indented = false)
        {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
        }

        /// <summary> 指定されたEnumに変換 </summary>
        public static T ToEnum<T>(this object obj)
        {
            try
            {
                if (!typeof(T).IsEnum)
                {
                    throw new InvalidOperationException();
                }

                if (obj is string)
                {
                    return (T)Enum.Parse(typeof(T), (string)obj);
                }

                return (T)Enum.ToObject(typeof(T), obj);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
