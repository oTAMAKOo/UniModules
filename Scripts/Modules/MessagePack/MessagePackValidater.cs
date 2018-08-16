﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using MessagePack;

namespace Modules.MessagePack
{
    public static class MessagePackValidater
    {
        /// <summary>
        /// <see cref="MessagePackObjectAttribute"/>がクラスに付与されているか検証.
        /// (デバッグビルド時のみ有効)
        /// </summary>
        /// <param name="type"></param>
        public static void ValidateAttribute(Type type)
        {
            if (!Debug.isDebugBuild) { return; }

            // 値型, クラス型, Enum, 文字列は除外.
            if (type.IsValueType || !type.IsClass || type.IsEnum || type == typeof(string)) { return; }

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
        }
    }
}