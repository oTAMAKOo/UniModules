
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using MessagePack;

namespace Extensions
{
    public class TypeGenerator
    {
        //----- params -----

        private const string DEFALT_CLASS_NAME = "NewClass";

        //  設定されているプロパティを取得.
        public interface ITypeData
        {
            string[] GetProperties();
        }

        //----- field -----

        //----- property -----

        public Type Type { get; private set; }
        public Dictionary<string, Type> Properties { get; private set; }

        //----- method -----

        /// <param name="properties">プロパティ情報</param>
        /// <example>
        /// <code>
        ///     Dictionary&lt;String,Type&gt; Properties = new Dictionary&lt;string,Type&gt;( );
        ///     Properties[ "AddPropertis1" ] = typeof( Double );
        ///     Properties[ "AddPropertis2" ] = typeof( int );
        ///     Properties[ "AddPropertis3" ] = typeof( Boolean );
        /// </code>
        /// </example>
        public TypeGenerator(Dictionary<string, Type> properties) : this(DEFALT_CLASS_NAME, properties){}

        public TypeGenerator(string className, Dictionary<string, Type> properties)
        {
            Type = Create(className, properties);
            Properties = properties;
        }

        private static Type Create(string className, Dictionary<string, Type> properties)
        {
            var domain = AppDomain.CurrentDomain;
            var assemblyName = new AssemblyName();

            assemblyName.Name = "TempAssembly.dll";

            var assemblyBuilder = domain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class, typeof(object), new Type[] { typeof(ITypeData) });

            // GetPropertiesを実装.
            var getPropsMethod = typeBuilder.DefineMethod("GetProperties", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string[]), Type.EmptyTypes);
            var getPropsIL = getPropsMethod.GetILGenerator();

            getPropsIL.DeclareLocal(typeof(string[]));
            LoadInteger(getPropsIL, properties.Count);
            getPropsIL.Emit(OpCodes.Newarr, typeof(string));
            getPropsIL.Emit(OpCodes.Stloc_0);

            for (var index = 0; index < properties.Count; index++)
            {
                getPropsIL.Emit(OpCodes.Ldloc_0);
                LoadInteger(getPropsIL, index);
                getPropsIL.Emit(OpCodes.Ldstr, properties.Keys.ElementAt(index));
                getPropsIL.Emit(OpCodes.Stelem_Ref);
            }

            getPropsIL.Emit(OpCodes.Ldloc_0);
            getPropsIL.Emit(OpCodes.Ret);

            var propAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // プロパティを作成.
            foreach (var Propertie in properties)
            {
                // プライベートフィールドの作成.
                var nameFieldBuilder = typeBuilder.DefineField(Propertie.Key + "_", Propertie.Value, FieldAttributes.Private);

                // パブリックプロパティ.
                var namePropertyBuilder = typeBuilder.DefineProperty(Propertie.Key, PropertyAttributes.HasDefault, Propertie.Value, null);

                //------ Getterメソッドの作成 ------

                MethodBuilder getNameMethod = typeBuilder.DefineMethod("get_" + Propertie.Key, propAttr, Propertie.Value, Type.EmptyTypes);
                ILGenerator getNamePropIL = getNameMethod.GetILGenerator();
                getNamePropIL.Emit(OpCodes.Ldarg_0);
                getNamePropIL.Emit(OpCodes.Ldfld, nameFieldBuilder);
                getNamePropIL.Emit(OpCodes.Ret);

                namePropertyBuilder.SetGetMethod(getNameMethod);

                //------ Setterメソッドの作成 ------

                var setNameMethod = typeBuilder.DefineMethod("set_" + Propertie.Key, propAttr, null, new Type[] { Propertie.Value });
                ILGenerator setNamePropIL = setNameMethod.GetILGenerator();
                setNamePropIL.Emit(OpCodes.Ldarg_0);
                setNamePropIL.Emit(OpCodes.Ldarg_1);
                setNamePropIL.Emit(OpCodes.Stfld, nameFieldBuilder);
                setNamePropIL.Emit(OpCodes.Ret);
                
                namePropertyBuilder.SetSetMethod(setNameMethod);
            }

            return typeBuilder.CreateType();
        }

        /// <summary>
        /// 指定したプロパティから値を取得する
        /// </summary>
        /// <param name="obj">取得する対象のインスタンス</param>
        /// <param name="propertyName">プロパティ名</param>
        /// <param name="type">タイプ</param>
        /// <returns>取得した値</returns>
        public static object GetProperty(object obj, string propertyName, Type type)
        {
            var info = obj.GetType().GetProperty(propertyName, type);

            if (info == null || !info.CanRead)
            {
                return null;
            }

            return info.GetValue(obj, null);
        }

        /// <summary>
        /// 指定したプロパティに値を設定する
        /// </summary>
        /// <param name="obj">取得する対象のインスタンス</param>
        /// <param name="propertyName">プロパティ名</param>
        /// <param name="value"></param>
        /// <param name="type">タイプ</param>
        public static void SetProperty(object obj, string propertyName, object value, Type type)
        {
            var info = obj.GetType().GetProperty(propertyName, type);

            if (info == null || !info.CanWrite)
            {
                return;
            }

            info.SetValue(obj, value, null);
        }

        public ITypeData NewInstance()
        {
            return Activator.CreateInstance(Type) as ITypeData;
        }

        private static void LoadInteger(ILGenerator il, int i)
        {
            switch (i)
            {
                case 0:  il.Emit(OpCodes.Ldc_I4_0); break;
                case 1:  il.Emit(OpCodes.Ldc_I4_1); break;
                case 2:  il.Emit(OpCodes.Ldc_I4_2); break;
                case 3:  il.Emit(OpCodes.Ldc_I4_3); break;
                case 4:  il.Emit(OpCodes.Ldc_I4_4); break;
                case 5:  il.Emit(OpCodes.Ldc_I4_5); break;
                case 6:  il.Emit(OpCodes.Ldc_I4_6); break;
                case 7:  il.Emit(OpCodes.Ldc_I4_7); break;
                case 8:  il.Emit(OpCodes.Ldc_I4_8); break;
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                default:
                    if (-128 <= i && i <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, i);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, i);
                    }
                    break;
            }
        }
    }
}
