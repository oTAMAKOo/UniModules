
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePack.Unity;

namespace Modules.MessagePack
{
    /// <summary>
    /// Editor時はDynamicResolver、実行時はGeneratedResolverの振る舞いをするResolver.
    /// </summary>
    public sealed class UnityCustomResolver : IFormatterResolver
    {
        public static IFormatterResolver Instance = new UnityCustomResolver();

		UnityCustomResolver() { }

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

        private static IFormatterResolver[] Resolvers()
        {
            return new IFormatterResolver[]
            {
                GeneratedResolver.Instance,
				
                DateTimeResolver.Instance,

                #if !NETSTANDARD1_4

                UnityResolver.Instance,

                #endif
                
                StandardResolver.Instance,
            };
        }

        //=============================================================================
        // ※ Dynamic***系は実機(iOS/Android)で使えないのでEditor時のみ使用する.
        //    実機でJsonFxのようにMap型を取り扱う際は必ずコード生成を行うようにする.
        //=============================================================================
        private static IFormatterResolver[] EditorResolvers()
        {
			return new IFormatterResolver[]
            {
				DateTimeResolver.Instance,

				BuiltinResolver.Instance,

				#if !NETSTANDARD1_4

				UnityResolver.Instance,

				#endif

				AttributeFormatterResolver.Instance,

				DynamicEnumResolver.Instance,

				DynamicGenericResolver.Instance,

				DynamicUnionResolver.Instance,
			
				DynamicObjectResolver.Instance,

				DynamicContractlessObjectResolver.Instance,

				PrimitiveObjectResolver.Instance,
            };
        }
    }
}
