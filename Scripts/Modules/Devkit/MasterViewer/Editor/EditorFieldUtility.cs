
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Extensions;

namespace Modules.Devkit.MasterViewer
{
    public static class EditorRecordFieldUtility
    {
        public static Type GetDisplayType(Type valueType)
        {
            var type = valueType;

            if (valueType.IsGenericType)
            {
                if (valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    type = Nullable.GetUnderlyingType(valueType);
                }
                else
                {
                    type = valueType.GetGenericArguments()[0];
                }
            }
            
            return type;
        }

        public static object DrawRecordField(object value, Type valueType, params GUILayoutOption[] options)
        {
            var type = GetDisplayType(valueType);

            object result = null;
                        
            if (value == null)
            {
                value = type.GetDefaultValue();
            }            

            var valueTypeTable = new Type[]
            {
                typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            };

            if (valueTypeTable.Contains(type))
            {
                result = EditorGUILayout.IntField(Convert.ToInt32(value), options);
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                result = EditorGUILayout.LongField(Convert.ToInt64(value), options);
            }
            else if (type == typeof(float))
            {
                result = EditorGUILayout.FloatField(Convert.ToSingle(value), options);
            }
            else if (type == typeof(double))
            {
                result = EditorGUILayout.DoubleField(Convert.ToDouble(value), options);
            }
            else if(type == typeof(bool))
            {
                result = EditorGUILayout.Toggle(Convert.ToBoolean(value), options);
            }
            else if (type == typeof(string))
            {
                var text = Convert.ToString(value).FixLineEnd();
                
                if (!string.IsNullOrEmpty(text))
                {
                    var lineCount = text.Count(c => c == '\n') + 1;

                    if (1 < lineCount)
                    {
                        var hight = EditorGUIUtility.singleLineHeight * Math.Min(3, lineCount);

                        var optionlist = options.ToList();

                        optionlist.Add(GUILayout.Height(hight));

                        options = optionlist.ToArray();

                        result = EditorGUILayout.TextArea(text, options);
                    }
                    else
                    {
                        result = EditorGUILayout.TextField(text, options);
                    }
                }
                else
                {
                    result = EditorGUILayout.TextField(text, options);
                }
            }
            else if (type == typeof(Vector2))
            {
                result = EditorGUILayout.Vector2Field(string.Empty, (Vector2)value, options);
            }
            else if (type == typeof(Vector3))
            {
                result = EditorGUILayout.Vector3Field(string.Empty, (Vector3)value, options);
            }
            else if (type == typeof(Vector4))
            {
                result = EditorGUILayout.Vector4Field(string.Empty, (Vector4)value, options);
            }
            else if (type.IsEnum)
            {
                result = EditorGUILayout.EnumPopup((Enum)value, options);
            }
            else
            {
                throw new NotSupportedException();
            }

            return result;
        }
    }
}
