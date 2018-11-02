﻿
using System;
using System.Reflection;

namespace Extensions
{
    public static class Reflection
    {
        /// <summary> Public属性の変数の値を取得 </summary>
        public static TResult GetPublicField<T, TResult>(T instance, string fieldName, BindingFlags? options = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var fieldInfo = typeof(T).GetField(fieldName, flags);

            return (TResult)fieldInfo.GetValue(instance);
        }

        /// <summary> Private属性の変数の値を取得 </summary>
        public static TResult GetPrivateField<T, TResult>(T instance, string fieldName, BindingFlags? options = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var fieldInfo = typeof(T).GetField(fieldName, flags);

            return (TResult)fieldInfo.GetValue(instance);
        }

        /// <summary> Public属性の変数に値を設定 </summary>
        public static void SetPublicField<T, TValue>(T instance, string fieldName, TValue value, BindingFlags? options = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Public | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var fieldInfo = typeof(T).GetField(fieldName, flags);

            fieldInfo.SetValue(instance, value);
        }

        /// <summary> Private属性の変数に値を設定 </summary>
        public static void SetPrivateField<T, TValue>(T instance, string fieldName, TValue value, BindingFlags? options = null)
        {
            var flags = BindingFlags.GetField | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var fieldInfo = typeof(T).GetField(fieldName, flags);

            fieldInfo.SetValue(instance, value);
        }

        /// <summary> Private属性のプロパティの値を取得 </summary>
        public static TResult GetPrivateProperty<T, TResult>(T instance, string propertyName, object[] index = null, BindingFlags? options = null)
        {
            var flags = BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var propertyInfo = typeof(T).GetProperty(propertyName, flags);

            return (TResult)propertyInfo.GetValue(instance, index);
        }

        /// <summary> Public属性のプロパティの値を取得 </summary>
        public static TResult GetPublicProperty<T, TResult>(T instance, string propertyName, object[] index = null, BindingFlags? options = null)
        {
            var flags = BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var propertyInfo = typeof(T).GetProperty(propertyName, flags);

            return (TResult)propertyInfo.GetValue(instance, index);
        }

        /// <summary> Private属性のプロパティに値を設定 </summary>
        public static void SetPrivateProperty<T, TValue>(T instance, string propertyName, TValue value, object[] index = null, BindingFlags? options = null)
        {
            var flags = BindingFlags.GetProperty | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var propertyInfo = typeof(T).GetProperty(propertyName, flags);

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
            var propertyInfo = typeof(T).GetProperty(propertyName);

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
        public static void InvokePrivateMethod<T>(T instance, string methodName, object[] parameters = null, BindingFlags? options = null)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var methodInfo = typeof(T).GetMethod(methodName, flags);

            methodInfo.Invoke(instance, parameters);
        }

        /// <summary> Public属性の関数を実行 </summary>
        public static void InvokePublicMethod<T>(T instance, string methodName, object[] parameters = null, BindingFlags? options = null)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;

            if (options.HasValue)
            {
                flags |= options.Value;
            }

            var methodInfo = typeof(T).GetMethod(methodName, flags);
            methodInfo.Invoke(instance, parameters);
        }
    }
}
