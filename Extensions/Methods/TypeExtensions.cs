
using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, string> AliasTable = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };

        public static bool IsNullable(this Type type)
        {
            return type == null || !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        public static string GetAliasName(this Type type)
        {
            return AliasTable.GetValueOrDefault(type, type.Name);
        }

        public static string GetFormattedName(this Type type)
        {
            if(type.IsGenericType)
            {
                var genericTypeName = type.Name.Substring(0, type.Name.IndexOf("`", StringComparison.Ordinal));

                var genericArguments = type.GetGenericArguments()
                    .Select(x => x.Name)
                    .Aggregate((x1, x2) => $"{x1}, {x2}");

                return genericTypeName + $"<{genericArguments}>";
            }

            return type.Name;
        }
    }
}