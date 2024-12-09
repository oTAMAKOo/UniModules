
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using MessagePack;
using Extensions;
using Modules.Master;
using Amazon.Runtime.Internal.Transform;

namespace Modules.Devkit.MasterViewer
{
    public sealed class MasterController
    {
        //----- params -----

        private const string CustomDataNameFormat = "[{0}]";

        private sealed class CustomDataInfo
        {
            public string name = null;

            public PropertyInfo propertyInfo = null;

            public CustomDataAttribute attribute = null;
        }

        //----- field -----

        private Dictionary<object, object> changedRecords = null;

        private Dictionary<string, PropertyInfo> propertyInfos = null;

        private Dictionary<string, CustomDataInfo> customDataInfos = null;

        private bool initialized = false;

        //----- property -----

        /// <summary> 編集可能か. </summary>
        public static bool CanEdit { get { return Application.isPlaying; } }

        /// <summary> データ型. </summary>
        public Type MasterType { get; private set; }

        /// <summary> レコード. </summary>
        public object[] Records { get; private set; }

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

            propertyInfos = new Dictionary<string, PropertyInfo>();
            customDataInfos = new Dictionary<string, CustomDataInfo>();

            var recordType = Reflection.GetElementTypeOfGenericEnumerable(allRecords);

            var properties = recordType.GetProperties();

            foreach (var property in properties)
            {
                // setter, getter両方が実装されてない場合は対象外.
                if (!property.CanRead || !property.CanWrite) { continue; }

                // IgnoreMemberがついている場合は対象外.
                if (property.CustomAttributes.Any(x => x.AttributeType == typeof(IgnoreMemberAttribute))) { continue; }

                propertyInfos.Add(property.Name, property);

                // CustomDataがついている場合は追加表示登録.

                var attributes = property.GetCustomAttributes(typeof(CustomDataAttribute))
                    .Select(x => x as CustomDataAttribute)
                    .OrderBy(x => x.Priority)
                    .ToArray();

                foreach (var attribute in attributes)
                {
                    var customDataName = string.Format(CustomDataNameFormat, attribute.Name);

                    var customDataInfo = new CustomDataInfo()
                    {
                        name = customDataName,
                        propertyInfo = property,
                        attribute = attribute,
                    };

                    customDataInfos.Add(customDataName, customDataInfo);
                }
            }

            initialized = true;
        }

        public void UpdateValue(object record, string valueName, object value)
        {
            // 非実行中は書き換え不可.

            if (!CanEdit)
            {
                EditorUtility.DisplayDialog("Require playing", "Editing values can only playing.", "Close");

                GUI.FocusControl(string.Empty);

                return;
            }

            // 対象の型に変換できない値は処理しない.
            
            var valueType = GetValueType(valueName);

            var convert = ConvertValue(ref value, valueType);

            if (!convert){ return; }

            // 元の値と同じなので処理しない.

            var currentValue = GetValue(record, valueName);

            if (currentValue == null && value == null) { return; }

            if (currentValue != null && value != null)
            {
                if (currentValue.Equals(value)) { return; }
            }

            // 変更情報を保存.

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

            // 書き換え.

            try
            {
                SetValue(record, valueName, value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // 書き換え前の値に戻ったら変更情報から除外.

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

        public void ResetAll()
        {
            var valueNames = GetValueNames();

            foreach (var record in Records)
            {
                foreach (var valueName in valueNames)
                {
                    ResetValue(record, valueName);
                }
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
            
            return originValue == null ? currentValue != null : !originValue.Equals(currentValue);
        }

        /// <summary> データ保持用レコードインスタンス生成. </summary>
        private object CreateRecordInstance(Type recordType, Dictionary<string, object> arguments)
        {
            return Activator.CreateInstance(recordType, arguments.Values.ToArray());
        }

        /// <summary> 値名取得. </summary>
        public string[] GetValueNames()
        {
            var names = new List<string>();

            foreach (var item in propertyInfos)
            {
                names.Add(item.Key);

                var customDataNames = customDataInfos.Values.Where(x => x.propertyInfo == item.Value).ToArray();

                if (customDataNames.IsEmpty()) { continue; }

                names.AddRange(customDataNames.Select(x => x.name));
            }

            return names.ToArray();
        }

        /// <summary> 値の型取得. </summary>
        public Type GetValueType(string valueName)
        {
            var propertyInfo = propertyInfos.GetValueOrDefault(valueName);

            if (propertyInfo != null)
            {
                return propertyInfo.PropertyType;
            }

            var customDataInfo = customDataInfos.GetValueOrDefault(valueName);

            if (customDataInfo != null)
            {
                return customDataInfo.attribute.GetDataType();
            }

            return null;
        }

        /// <summary> 値の設定. </summary>
        public void SetValue(object record, string valueName, object value)
        {
            var propertyInfo = propertyInfos.GetValueOrDefault(valueName);
            
            var convert = ConvertValue(ref value, propertyInfo.PropertyType);

            if (convert)
            {
                propertyInfo.SetValue(record, value);
            }
        }

        /// <summary> 値の取得. </summary>
        public object GetValue(object record, string valueName)
        {
            var propertyInfo = propertyInfos.GetValueOrDefault(valueName);

            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(record);
            }

            var customDataInfo = customDataInfos.GetValueOrDefault(valueName);

            if (customDataInfo != null)
            {
                var value = customDataInfo.propertyInfo.GetValue(record);

                return customDataInfo.attribute.GetCustomData(record, value);
            }

            return null;
        }

        /// <summary> マスター表示名取得. </summary>
        public string GetDisplayMasterName()
        {
            const string MasterSuffix = "Master";

            var masterName = MasterType.Name;

            if (masterName.EndsWith(MasterSuffix))
            {
                masterName = masterName.SafeSubstring(0, masterName.Length - MasterSuffix.Length);
            }

            return masterName;
        }

        private static bool ConvertValue(ref object value, Type valueType)
        {
            var isNullableType = valueType.IsNullable();

            // Null非許容型にnullが来た時は失敗.
            if (value == null && !isNullableType) { return false; }

            try
            {
                if (value != null)
                {
                    var type = valueType;

                    if (isNullableType)
                    {
                        type = Nullable.GetUnderlyingType(valueType);

                        if (type == null)
                        {
                            type = valueType;
                        }
                    }

                    if (value.GetType() != type)
                    {
                        value = Convert.ChangeType(value, type);
                    }
                }
            }
            catch (Exception e)
            {
                var message = "Value convert failed.\nvalue : {0}\ntype : {1} -> {2}\n\n{3}";

                Debug.LogErrorFormat(message, value, value != null ? value.GetType().ToString() : "null", valueType, e.Message);

                return false;
            }

            return true;
        }

        public bool IsCustomData(string valueName)
        {
            return customDataInfos.Values.Any(x => x.name == valueName);
        }
    }
}
