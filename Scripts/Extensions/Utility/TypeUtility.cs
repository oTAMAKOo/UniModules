
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;

namespace Extensions
{
    public static class TypeUtility
    {
        //----- params -----

        //----- field -----

        private static Dictionary<string, Type> TypeTable = null;

        //----- property -----

        //----- method -----

        public static void CreateTypeTable()
        {
            if (TypeTable != null) { return; }

            TypeTable = new Dictionary<string, Type>();

            var mscorlib = Assembly.GetAssembly(typeof(int));

            using (var provider = new CSharpCodeProvider())
            {
                foreach (var definedType in mscorlib.DefinedTypes)
                {
                    var typeRef = new CodeTypeReference(definedType);
                    var typeName = provider.GetTypeOutput(typeRef);

                    if (string.Equals(definedType.Namespace, "System"))
                    {
                        // int, float, stringなどの短縮型.
                        if (typeName.IndexOf('.') == -1)
                        {
                            TypeTable.Add(typeName, Type.GetType(definedType.FullName));
                        }
                        else
                        {
                            typeName = typeName.Substring(definedType.Namespace.Length + 1);

                            TypeTable.Add(typeName, Type.GetType(definedType.FullName));
                        }
                    }
                }
            }
        }

        /// <summary> System名前空間内の型の名前から型情報を取得 </summary>
        public static Type GetTypeFromSystemTypeName(string csTypeName)
        {
            csTypeName = csTypeName.Trim();

            // 型テーブル生成.
            CreateTypeTable();

            // 配列.
            var array = csTypeName.EndsWith("[]");

            if (array)
            {
                csTypeName = csTypeName.Substring(0, csTypeName.Length - 2);
            }

            // Null許容型.
            var nullable = csTypeName.EndsWith("?");

            if (nullable)
            {
                csTypeName = csTypeName.TrimEnd('?');
            }

            // 型情報を生成.

            var type = TypeTable.GetValueOrDefault(csTypeName);

            if (array)
            {
                type = type.MakeArrayType();
            }

            if (nullable)
            {
                type = typeof(Nullable<>).MakeGenericType(type);
            }

            return type;
        }

        /// <summary> 型情報からデフォルト値を取得 </summary>
        public static object GetDefaultValue(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
