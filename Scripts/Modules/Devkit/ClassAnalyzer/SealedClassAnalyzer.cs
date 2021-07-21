
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Modules.Devkit.ClassAnalyzer
{
    public static class SealedClassAnalyzer
    {
        public static Type[] SearchTypes(Type assemblyClassType, string[] nameSpace, Type[] ignoreTypes)
        {
            // 現在のコードを実行しているアセンブリを取得する
            var assembly = Assembly.GetAssembly(assemblyClassType);

            // 全ての型取得.

            var allTypes = assembly.GetTypes().ToArray();

            // 継承されているクラスを除外.

            var baseTypes = new HashSet<Type>();

            foreach (var type in allTypes)
            {
                if (type.BaseType == null) { continue; }

                var baseType = type.BaseType;

                if (baseType.IsGenericType)
                {
                    baseType = baseType.GetGenericTypeDefinition();
                }

                if (baseTypes.Contains(baseType)) { continue; }
                
                baseTypes.Add(baseType);
            }

            var types = allTypes.Where(x => !baseTypes.Contains(x)).ToArray();

            // 対象になる型を取得.

            Func<Type, bool> filter = x =>
            {
                // class以外は除外.
                if (!x.IsClass) { return false; }

                // abstract修飾子付きは除外.
                if (x.IsAbstract) { return false; }

                // sealed修飾子付きは除外.
                if (x.IsSealed) { return false; }

                // 名前空間がない場合は除外.
                if (string.IsNullOrEmpty(x.Namespace)) { return false; }

                // 指定された名前空間以外は除外.
                if (nameSpace.All(y => !x.Namespace.StartsWith(y))) { return false; }

                // virtualプロパティがあったら除外.

                Func<PropertyInfo, bool> checkProperty = p =>
                {
                    var accessor = p.GetAccessors()[0];

                    return p.DeclaringType == x && accessor.IsVirtual;
                };

                if (x.GetProperties().Any(p => checkProperty.Invoke(p))) { return false; }

                // virtual関数があったら除外.

                Func<MethodInfo, bool> checkMethod = m =>
                {
                    return m.DeclaringType == x && m.IsVirtual;
                };

                if (x.GetMethods().Any(m => checkMethod.Invoke(m))) { return false; }

                // 除外対象の型.
                if (ignoreTypes.Contains(x)) { return false; }

                return true;
            };

            types = types.Where(x => filter.Invoke(x)).ToArray();

            return types;
        }
    }
}
