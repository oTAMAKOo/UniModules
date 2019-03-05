
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

        /// <summary> 型の名前から型情報を取得 </summary>
        public static Type GetTypeFromTypeName(string csTypeName)
        {
            csTypeName = csTypeName.Trim();

            var nullable = csTypeName.EndsWith("?");

            if (nullable)
            {
                csTypeName = csTypeName.TrimEnd('?');
            }

            if (TypeTable == null)
            {
                TypeTable = new Dictionary<string, Type>();

                var mscorlib = Assembly.GetAssembly(typeof(int));

                using (var provider = new CSharpCodeProvider())
                {
                    foreach (var definedType in mscorlib.DefinedTypes)
                    {
                        if (string.Equals(definedType.Namespace, "System"))
                        {
                            var typeRef = new CodeTypeReference(definedType);
                            var typeName = provider.GetTypeOutput(typeRef);

                            if (typeName.IndexOf('.') == -1)
                            {
                                TypeTable.Add(typeName, Type.GetType(definedType.FullName));
                            }
                        }
                    }
                }
            }

            var type = TypeTable.GetValueOrDefault(csTypeName);

            return nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
        }
    }
}
