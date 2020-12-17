
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Extensions;
using Modules.Master;

namespace Modules.Devkit.MasterViewer
{
    public class MasterController
    {
        //----- params -----

        //----- field -----

        private Dictionary<object, object> changedRecords = null;

        protected Dictionary<string, PropertyInfo> propertyInfos = null;

        private bool initialized = false;

        //----- property -----

        /// <summary> データ型. </summary>
        public Type MasterType { get; private set; }

        /// <summary> レコード. </summary>
        public object[] Records { get; private set; }

        /// <summary> フィールド幅. </summary>
        public float[] FieldWidth { get; private set; }

        /// <summary> 編集済みレコードがあるか. </summary>
        public bool HasChangedRecord { get { return changedRecords.Any(); } }

        //----- method -----

        public void Initialize(IMaster master)
        {
            if (initialized) { return; }

            changedRecords = new Dictionary<object, object>();

            MasterType = master.GetType();

            // レコード一覧取得.

            var methodInfo = MasterType.GetMethods().FirstOrDefault(x => x.Name == "GetAllRecords");

            var allRecords = (IEnumerable)methodInfo.Invoke(master, null);

            Records = allRecords.Cast<object>().ToArray();

            // レコードのプロパティ情報取得.

            var recordType = Reflection.GetElementTypeOfGenericEnumerable(allRecords);

            propertyInfos = recordType.GetProperties().ToDictionary(x => x.Name);

            // フィールド幅計算.

            var valueNames = GetValueNames();

            FieldWidth = new float[valueNames.Length];

            for (var i = 0; i < valueNames.Length; i++)
            {
                var content = new GUIContent(valueNames[i]);
                var size = EditorStyles.label.CalcSize(content);

                FieldWidth[i] = Mathf.Max(80f, size.x + 20f);
            }

            initialized = true;
        }

        public void UpdateValue(object record, string valueName, object value)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Require playing", "Editing values can only playing.", "Close");

                GUI.FocusControl(string.Empty);

                return;
            }

            var currentValue = GetValue(record, valueName);

            if (currentValue.Equals(value)) { return; }

            var originData = changedRecords.GetValueOrDefault(record);

            var valueNames = GetValueNames();

            if (originData == null)
            {
                var recordType = record.GetType();

                var arguments = valueNames.ToDictionary(x => x, x => GetValue(record, x));

                originData = CreateRecordInstance(recordType, arguments);

                // 編集前の元データをコピー.
                valueNames.ForEach(x => SetValue(originData, x, GetValue(record, x)));

                changedRecords.Add(record, originData);
            }

            try
            {
                SetValue(record, valueName, value);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "close");
            }

            var hasChanged = false;

            for (var i = 0; i < valueNames.Length; i++)
            {
                if (IsChanged(record, valueNames[i]))
                {
                    hasChanged = true;
                    break;
                }
            }

            if (!hasChanged)
            {
                changedRecords.Remove(record);
            }
        }

        public void ResetValue(object record, string valueName)
        {
            var originData = changedRecords.GetValueOrDefault(record);

            if (originData == null) { return; }

            var originValue = GetValue(originData, valueName);

            UpdateValue(record, valueName, originValue);
        }

        public bool IsChanged(object record, string valueName)
        {
            var originData = changedRecords.GetValueOrDefault(record);

            if (originData == null) { return false; }

            var valueType = GetValueType(valueName);

            var interfaces = valueType.GetInterfaces();

            var originValue = GetValue(originData, valueName);
            var currentValue = GetValue(record, valueName);

            if (interfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                var originArray = new ArrayList();
                var currentArray = new ArrayList();

                if (originValue != null)
                {
                    foreach (var item in (IEnumerable)originValue)
                    {
                        originArray.Add(item);
                    }
                }

                if (currentValue != null)
                {
                    foreach (var item in (IEnumerable)currentValue)
                    {
                        currentArray.Add(item);
                    }
                }

                if (originArray.Count != currentArray.Count) { return true; }
                
                for (var i = 0; i < originArray.Count; i++)
                {
                    var v1 = originArray[i];
                    var v2 = currentArray[i];

                    if (!Equals(v1, v2)) { return true; }
                }

                return false;
            }

            if (valueType.IsGenericType)
            {
                if (valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var displayType = EditorRecordFieldUtility.GetDisplayType(valueType);

                    if (originValue == null)
                    {
                        originValue = displayType.GetDefaultValue();
                    }

                    if (currentValue == null)
                    {
                        currentValue = displayType.GetDefaultValue();
                    }
                }
            }

            return originValue == null ? currentValue != null : !originValue.Equals(currentValue);
        }

        /// <summary> データ保持用レコードインスタンス生成. </summary>
        protected object CreateRecordInstance(Type recordType, Dictionary<string, object> arguments)
        {
            return Activator.CreateInstance(recordType, arguments.Values.ToArray());
        }

        /// <summary> 値名取得. </summary>
        public string[] GetValueNames()
        {
            return propertyInfos.Keys.ToArray();
        }

        /// <summary> 値の型取得. </summary>
        public Type GetValueType(string valueName)
        {
            var propertyInfo = propertyInfos.GetValueOrDefault(valueName);

            return propertyInfo.PropertyType;
        }

        /// <summary> 値の設定. </summary>
        public void SetValue(object record, string valueName, object value)
        {
            var propertyInfo = propertyInfos.GetValueOrDefault(valueName);

            propertyInfo.SetValue(record, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

        /// <summary> 値の取得. </summary>
        public object GetValue(object record, string valueName)
        {
            var propertyInfo = propertyInfos.GetValueOrDefault(valueName);

            return propertyInfo.GetValue(record);
        }

        /// <summary> マスター表示名取得. </summary>
        public virtual string GetDisplayMasterName()
        {
            const string MasterSuffix = "Master";

            var masterName = MasterType.Name;

            if (masterName.EndsWith(MasterSuffix))
            {
                masterName = masterName.SafeSubstring(0, masterName.Length - MasterSuffix.Length);
            }

            return masterName;
        }
    }
}
