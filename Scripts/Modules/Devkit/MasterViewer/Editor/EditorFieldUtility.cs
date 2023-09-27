
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.MasterViewer
{
    public static class EditorRecordFieldUtility
    {
        private static readonly Type[] ValueTypeTable = new Type[]
        {
            typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        };
    
        public static bool IsArrayType(Type valueType)
        {
            if (valueType.IsArray) { return true; }

            if (valueType.IsGenericType)
            {
                var genericTypeDefinition = valueType.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(IList<>))
                {
                    return true;
                }

                if (genericTypeDefinition == typeof(List<>))
                {
                    return true;
                }
            }

            return false;
        }

        public static Type GetDisplayType(Type valueType)
        {
            var type = valueType;

            if (valueType.IsGenericType)
            {
                var genericTypeDefinition = valueType.GetGenericTypeDefinition();

                type = genericTypeDefinition == typeof(Nullable<>) ?
                       Nullable.GetUnderlyingType(valueType) :
                       valueType.GetGenericArguments()[0];
            }

            if (valueType.IsArray)
            {
                type = valueType.GetElementType();
            }

            return type;
        }

        public static object DrawField(Rect rect, object value, Type valueType)
        {
            var type = GetDisplayType(valueType);
            
            object result = null;

            // Nullable Field.

            if (valueType != typeof(string) && valueType.IsNullable())
            {
                const float ToggleWidth = 20f;

                var toggleRect = new Rect(rect);
                
                toggleRect.width = ToggleWidth;

                var isNull = value == null;

                var toggle = EditorGUI.Toggle(toggleRect, !isNull);

                if (isNull == toggle)
                {
                    value = toggle ? type.GetDefaultValue() : null;

                    result = value;
                }

                rect.width -= ToggleWidth;
                rect.x += ToggleWidth;
            }

            // Value Field.

            if (value == null)
            {
                using (new DisableScope(true))
                {
                    EditorGUI.TextField(rect, "null");
                }
            }
            else if (ValueTypeTable.Contains(type))
            {
                result = EditorGUI.IntField(rect, Convert.ToInt32(value));
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                result = EditorGUI.LongField(rect, Convert.ToInt64(value));
            }
            else if (type == typeof(float))
            {
                result = EditorGUI.FloatField(rect, Convert.ToSingle(value));
            }
            else if (type == typeof(double))
            {
                result = EditorGUI.DoubleField(rect, Convert.ToDouble(value));
            }
            else if (type == typeof(bool))
            {
                result = EditorGUI.Toggle(rect, Convert.ToBoolean(value));
            }
            else if (type == typeof(string))
            {
                var text = Convert.ToString(value);

                result = EditorGUI.TextArea(rect, text);
            }
            else if (type == typeof(Vector2))
            {
                result = EditorGUI.Vector2Field(rect, string.Empty, (Vector2)value);
            }
            else if (type == typeof(Vector3))
            {
                result = EditorGUI.Vector3Field(rect, string.Empty, (Vector3)value);
            }
            else if (type == typeof(Vector4))
            {
                result = EditorGUI.Vector4Field(rect, string.Empty, (Vector4)value);
            }
            else if (type == typeof(DateTime))
            {
                var dateTime = (DateTime)value;

                var from = Convert.ToString(value);

                var to = EditorGUI.DelayedTextField(rect, string.Empty, from);

                result = value;

                if (from != to)
                {
                    result = DateTime.TryParse(to, out var parseValue) ? parseValue : dateTime;
                }
            }
            else if (type.IsEnum)
            {
                result = EditorGUI.EnumPopup(rect, (Enum)value);
            }
            else
            {
                throw new NotSupportedException();
            }

            return result;
        }
    }
}
