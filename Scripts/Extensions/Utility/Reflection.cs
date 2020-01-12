﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Extensions
{
    public static class Reflection
    {
        /// <summary> Public属性の変数の値を取得 </summary>
        public static TResult GetPublicField<T, TResult>(T instance, string fieldName, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var fieldInfo = GetFieldInfo(typeof(T), fieldName, flags);

            return (TResult)fieldInfo.GetValue(instance);
        }

        /// <summary> Private属性の変数の値を取得 </summary>
        public static TResult GetPrivateField<T, TResult>(T instance, string fieldName, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var fieldInfo = GetFieldInfo(typeof(T), fieldName, flags);

            return (TResult)fieldInfo.GetValue(instance);
        }

        /// <summary> Public属性の変数に値を設定 </summary>
        public static void SetPublicField<T, TValue>(T instance, string fieldName, TValue value, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Public | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var fieldInfo = GetFieldInfo(typeof(T), fieldName, flags);

            fieldInfo.SetValue(instance, value);
        }

        /// <summary> Private属性の変数に値を設定 </summary>
        public static void SetPrivateField<T, TValue>(T instance, string fieldName, TValue value, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var fieldInfo = GetFieldInfo(typeof(T), fieldName, flags);

            fieldInfo.SetValue(instance, value);
        }

        /// <summary> Private属性のプロパティの値を取得 </summary>
        public static TResult GetPrivateProperty<T, TResult>(T instance, string propertyName, object[] index = null, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var propertyInfo = GetPropertyInfo(typeof(T), propertyName, flags);

            return (TResult)propertyInfo.GetValue(instance, index);
        }

        /// <summary> Public属性のプロパティの値を取得 </summary>
        public static TResult GetPublicProperty<T, TResult>(T instance, string propertyName, object[] index = null, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var propertyInfo = GetPropertyInfo(typeof(T), propertyName, flags);

            return (TResult)propertyInfo.GetValue(instance, index);
        }

        /// <summary> Private属性のプロパティに値を設定 </summary>
        public static void SetPrivateProperty<T, TValue>(T instance, string propertyName, TValue value, object[] index = null, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.GetProperty | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var propertyInfo = GetPropertyInfo(typeof(T), propertyName, flags);

            if (propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(instance, value, index);
            }
            else
            {
                throw new InvalidOperationException("This property can not be written");
            }
        }

        /// <summary> Public属性のプロパティに値を設定 </summary>
        public static void SetPublicProperty<T, TValue>(T instance, string propertyName, TValue value, object[] index = null)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;

            var propertyInfo = GetPropertyInfo(typeof(T), propertyName, flags);

            if (propertyInfo.CanWrite)
            {
                propertyInfo.SetValue(instance, value, index);
            }
            else
            {
                throw new InvalidOperationException("This property can not be written");
            }
        }

        /// <summary> Private属性の関数を実行 </summary>
        public static object InvokePrivateMethod<T>(T instance, string methodName, object[] parameters = null, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var methodInfo = GetMethodInfo(typeof(T), methodName, flags);

            return methodInfo.Invoke(instance, parameters);
        }

        /// <summary> Public属性の関数を実行 </summary>
        public static object InvokePublicMethod<T>(T instance, string methodName, object[] parameters = null, BindingFlags? bindingFlags = null)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;

            if (bindingFlags.HasValue)
            {
                flags |= bindingFlags.Value;
            }

            var methodInfo = GetMethodInfo(typeof(T), methodName, flags);

            return methodInfo.Invoke(instance, parameters);
        }

        /// <summary> 変数情報を取得 </summary>
        public static FieldInfo GetFieldInfo(Type type, string fieldName, BindingFlags bindingFlags)
        {
            var info = type.GetField(fieldName, bindingFlags);

            if (info != null) { return info; }

            if (type.BaseType != null)
            {
                return GetFieldInfo(type.BaseType, fieldName, bindingFlags);
            }

            return null;
        }

        /// <summary> プロパティ情報を取得 </summary>
        public static PropertyInfo GetPropertyInfo(Type type, string propertyName, BindingFlags bindingFlags)
        {
            var info = type.GetProperty(propertyName, bindingFlags);

            if (info != null) { return info; }

            if (type.BaseType != null)
            {
                return GetPropertyInfo(type.BaseType, propertyName, bindingFlags);
            }

            return null;
        }

        /// <summary> 関数情報を取得 </summary>
        public static MethodInfo GetMethodInfo(Type type, string methodName, BindingFlags bindingFlags)
        {
            var info = type.GetMethod(methodName, bindingFlags);

            if (info != null) { return info; }

            if (type.BaseType != null)
            {
                return GetMethodInfo(type.BaseType, methodName, bindingFlags);
            }

            return null;
        }

        /// <summary> ジェネリック型配列の要素型取得 </summary>
        public static Type GetElementTypeOfGenericEnumerable(object instance)
        {
            var enumerable = instance as IEnumerable;

            if (enumerable == null) { return null; }

            var interfaces = enumerable.GetType().GetInterfaces();

            return interfaces.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(x => x.GetGenericArguments()[0])
                .FirstOrDefault();
        }
    }
}
