
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Extensions
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class LabelAttribute : System.Attribute
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

    public static class LabelAttributeUtility
    {
        private static readonly Dictionary<Type, Dictionary<object, Dictionary<int, string>>> labelCache = null;

        static LabelAttributeUtility()
        {
            labelCache = new Dictionary<Type, Dictionary<object, Dictionary<int, string>>>();
        }

        public static string ToLabelName(object value, Type type, int no)
        {
            Dictionary<object, Dictionary<int, string>> attributes;

            lock (labelCache)
            {
                if (!labelCache.TryGetValue(type, out attributes))
                {
                    attributes = GetAttributeInfo(type);
    
                    labelCache.Add(type, attributes);
                }
            }

            if (attributes.TryGetValue(value, out var labels))
            {
                if (labels.TryGetValue(no, out var name)) { return name; }
            }

            return null;
        }

        private static Dictionary<object, Dictionary<int, string>> GetAttributeInfo(Type type)
        {
            var fields = type.GetFields();

            Func<FieldInfo, Dictionary<int, string>> getLabels = fi =>
            {
                return fi.GetCustomAttributes(true)
                    .Where(y => y is LabelAttribute)
                    .Cast<LabelAttribute>()
                    .ToDictionary(attr => attr.No, attr => attr.Name);
            };

            var attributes = fields
                .Where(fi => fi.FieldType == type)
                .Select(x =>
                    {
                        var value = x.GetValue(null);
                        var labels = getLabels.Invoke(x);

                        return new { Value = value, Labels = labels };
                    })
                .Where(x => x.Labels.Any())
                .ToDictionary(x => x.Value, x => x.Labels);

            return attributes;
        }
    }
}