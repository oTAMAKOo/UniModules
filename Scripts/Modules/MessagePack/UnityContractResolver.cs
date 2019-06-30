﻿
using System;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePack.Unity;

namespace Modules.MessagePack
{
    /// <summary>
    /// Editor時はDynamicResolver、実行時はGeneratedResolverの振る舞いをするResolver.
    /// </summary>
    public sealed class UnityContractResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new UnityContractResolver();

        UnityContractResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                IFormatterResolver[] resolvers = null;

                #if UNITY_EDITOR

                resolvers = EditorResolvers();

                #else

                resolvers = Resolvers();
                
                #endif

                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }

        #if !UNITY_EDITOR

        private static IFormatterResolver[] Resolvers()
        {
            return new IFormatterResolver[]
            {
                // DateTime.
                DateTimeResolver.Instance,

                GeneratedResolver.Instance,

                // Builtin.
                BuiltinResolver.Instance,

                #if !NETSTANDARD1_4

                // Vector2, Vector3, Quaternion, Color, Bounds, Rect.
                UnityResolver.Instance,

                #endif

                // Primitive.
                PrimitiveObjectResolver.Instance,
            };
        }

        #endif

        //=============================================================================
        // ※ Dynamic***系は実機(iOS/Android)で使えないのでEditor時のみ使用する.
        //    実機でJsonFxのようにMap型を取り扱う際は必ずコード生成を行うようにする.
        //=============================================================================
        private static IFormatterResolver[] EditorResolvers()
        {
            return new IFormatterResolver[]
            {
                // DateTime.
                DateTimeResolver.Instance,

                // Builtin.
                BuiltinResolver.Instance,

                #if !NETSTANDARD1_4

                // Vector2, Vector3, Quaternion, Color, Bounds, Rect.
                UnityResolver.Instance,

                #endif

                // Enum.
                DynamicEnumResolver.Instance,

                // Array, Tuple, Collection.
                DynamicGenericResolver.Instance,

                // Union(Interface).
                DynamicUnionResolver.Instance,
                
                // Object (Map Mode).
                DynamicContractlessObjectResolver.Instance,

                // Primitive.
                PrimitiveObjectResolver.Instance,
            };
        }
    }
}
