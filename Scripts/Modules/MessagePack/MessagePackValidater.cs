
using System;
using System.Collections.Generic;
using System.Diagnostics;
using MessagePack;

namespace Modules.MessagePack
{
    public static class MessagePackValidater
    {
        /// <summary> <see cref="MessagePackObjectAttribute"/>がクラスに付与されているか検証. (Editor時のみ有効) </summary>
        [Conditional("UNITY_EDITOR")]
        public static void ValidateAttribute(Type type)
        {
            #if UNITY_EDITOR
            
            // 配列.
            if (type.IsArray)
            {
                type = type.GetElementType();
            }

            // リスト.
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                type = type.GetGenericArguments()[0];
            }

            // 値型, Enum, 文字列は除外.
            if (type.IsValueType || type.IsEnum || type == typeof(string)) { return; }

            var attribute = Attribute.GetCustomAttribute(type, typeof(MessagePackObjectAttribute), false) as MessagePackObjectAttribute;

            if (attribute == null)
            {
                throw new InvalidOperationException(string.Format("Attribute error [MessagePackObject] not found!\nclass : {0}\n", type.FullName));
            }

            // ジェネレートする為に公開されたクラスでないといけない.
            if ((!type.IsNested && !type.IsPublic) || (type.IsNested && !type.IsNestedPublic))
            {
                throw new InvalidOperationException(string.Format("Accessibility error [MessagePackObject] is need public!\nclass : {0}\n", type.FullName));
            }

            var fields = type.GetFields();

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                // クラス型以外は除外.
                if (!fieldType.IsClass) { continue; }

                // 文字列は除外.
                if (fieldType == typeof(string)) { continue; }

                // Enumは除外.
                if (fieldType.IsEnum) { continue; }

                // クラスを検証.
                ValidateAttribute(fieldType.IsArray ? fieldType.GetElementType() : fieldType);
            }

            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;

                // クラス型以外は除外.
                if (!propertyType.IsClass) { continue; }

                // 文字列は除外.
                if (propertyType == typeof(string)) { continue; }

                // Enumは除外.
                if (propertyType.IsEnum) { continue; }

                // クラスを検証.
                ValidateAttribute(propertyType.IsArray ? propertyType.GetElementType() : propertyType);
            }

            #endif
        }
    }
}
