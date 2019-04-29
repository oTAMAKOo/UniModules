
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class LabelAttribute : System.Attribute
    {
        public int No { get; private set; }
        public string Name { get; private set; }

        //
        // 概要:
        //  .ToLabelName()拡張メソッドで取り出せる名前を指定します。
        public LabelAttribute(string name, int no = 0)
        {
            Name = name;
            No = no;
        }
    }

    public static class EnumExtensions
    {
        private static readonly Dictionary<Type, Dictionary<object, Dictionary<int, string>>> labelCache = new Dictionary<Type, Dictionary<object, Dictionary<int, string>>>();

        /// <summary>
        /// EnumにLabelAttributeで指定された名前を取り出します
        /// </summary>
        public static string ToLabelName(this Enum @enum, int no = 0)
        {
            var type = @enum.GetType();

            Dictionary<object, Dictionary<int, string>> attributes;
            lock (labelCache)
            {
                if (!labelCache.TryGetValue(type, out attributes))
                {
                    var dict = type.GetFields()
                        .Where(fi => fi.FieldType == type)
                        .Select(x =>
                        {
                            var value = x.GetValue(null);
                            var enumLabels = x.GetCustomAttributes(true)
                                .Where(y => y.GetType().Name == "LabelAttribute")
                                .ToDictionary(attr => (int)attr.GetType().GetProperty("No").GetGetMethod(false).Invoke(attr, null), attr => (string)attr.GetType().GetProperty("Name").GetGetMethod(false).Invoke(attr, null));

                            return new { Value = value, Labels = enumLabels };
                        })
                        .Where(x => x.Labels.Any())
                        .ToDictionary(x => x.Value, x => x.Labels);
                    labelCache.Add(type, dict);
                    attributes = dict;
                }
            }

            Dictionary<int, string> labels;
            if (attributes.TryGetValue(@enum, out labels))
            {
                string name;
                if (labels.TryGetValue(no, out name))
                {
                    return name;
                }
                else
                {
                    throw new ArgumentException("対象のEnum" + "「" + @enum.GetType() + ":" + @enum + "」に指定されているLabelAttributeに一致するNo:\"" + no + "\"がありません");
                }
            }

            return null;
        }

        public static bool HasFlag<T>(this T source, T destination) where T : struct, IComparable, IFormattable, IConvertible
        {
            return (Convert.ToUInt64(source) & Convert.ToUInt64(destination)) != 0;
        }
    }
}
