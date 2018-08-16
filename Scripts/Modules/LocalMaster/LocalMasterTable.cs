﻿
using UnityEngine;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using Extensions;
using Extensions.Serialize;

namespace Modules.LocalMaster
{
    public abstract class LocalMasterAsset : ScriptableObject
    {
        //----- params -----

        //----- field -----

        [SerializeField, ReadOnly]
        public IntNullable updateTime = null;

        //----- property -----

        public int? UpdateTime { get { return updateTime; } }

        //----- method -----

        public void SetUpdateTime(long lastUpdateDate)
        {
            updateTime = Convert.ToInt32(lastUpdateDate);
        }

        public abstract void TableInsert(Dictionary<string, string> param);
        public abstract void TableClear();
    }

    public class LocalMasterAsset<T> : LocalMasterAsset where T : new()
    {
        //----- params -----

        //----- field -----

        [SerializeField, ReadOnly]
        protected List<T> table = null;

        //----- property -----

        public T[] Table { get { return table.ToArray(); } }

        //----- method -----

        protected LocalMasterAsset()
        {
            table = new List<T>();
        }

        public override void TableInsert(Dictionary<string, string> tableParam)
        {
            var item = new T();

            var line = 0;

            foreach (var param in tableParam)
            {
                var fieldInfo = typeof(T).GetField(param.Key, 
                    BindingFlags.GetField | BindingFlags.SetField | 
                    BindingFlags.Public | BindingFlags.NonPublic | 
                    BindingFlags.Instance);

                // ※ 他の型にも対応させたいときにはここに型を追加.

                try
                {
                    if(fieldInfo.FieldType == typeof(IntNullable))
                    {
                        var value = string.IsNullOrEmpty(param.Value) ? new IntNullable(null) : new IntNullable(int.Parse(param.Value));
                        fieldInfo.SetValue(item, value);
                    }
                    else if(fieldInfo.FieldType == typeof(FloatNullable))
                    {
                        var value = string.IsNullOrEmpty(param.Value) ? new FloatNullable(null) : new FloatNullable(float.Parse(param.Value));
                        fieldInfo.SetValue(item, value);
                    }
                    else if(fieldInfo.FieldType == typeof(int))
                    {
                        fieldInfo.SetValue(item, int.Parse(param.Value));
                    }
                    else if(fieldInfo.FieldType == typeof(float))
                    {
                        fieldInfo.SetValue(item, float.Parse(param.Value));
                    }
                    else if (fieldInfo.FieldType == typeof(string))
                    {
                        fieldInfo.SetValue(item, param.Value);
                    }
                    else if(fieldInfo.FieldType.BaseType == typeof(Enum))
                    {
                        fieldInfo.SetValue(item, Enum.Parse(fieldInfo.FieldType, param.Value));
                    }
                    else if(fieldInfo.FieldType == typeof(bool))
                    {
                        if(param.Value == "0" || param.Value == "1")
                        {
                            fieldInfo.SetValue(item, param.Value == "1");
                        }
                        else
                        {
                            fieldInfo.SetValue(item, bool.Parse(param.Value));
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("識別できない型「{0}」(= {1})が使用されています.", param.Key, param.Value);
                    }
                }
                catch(Exception e)
                {
                    if(e is FormatException)
                    {
                        Debug.LogErrorFormat("[line:{0}] 定義された型と値の型が一致しません。\nparam={1} value={2}", line, param.Key, param.Value);
                    }
                    else
                    {
                        Debug.LogException(e);
                    }
                }

                line++;
            }

            table.Add(item);
        }

        public override void TableClear()
        {
            table.Clear();
        }
    }
}