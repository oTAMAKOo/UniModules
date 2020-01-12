
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.Devkit.MasterViewer
{
    public abstract class MasterController
    {
        //----- params -----
        
        //----- field -----
        
        private Dictionary<object, object> changedRecords = null;

        //----- property -----

        /// <summary> データ型. </summary>
        public Type MasterType { get; set; }

        /// <summary> レコード. </summary>
        public object[] Records { get; set; }

        /// <summary> 編集済みレコードがあるか. </summary>
        public bool HasChangedRecord { get { return changedRecords.Any(); } }

        /// <summary> 編集可能か. </summary>
        public bool EnableEdit { get; set; }

        //----- method -----

        public MasterController()
        {
            changedRecords = new Dictionary<object, object>();

            Records = new object[0];
        }

        public void UpdateValue(object record, string valueName, object value)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Require playing", "Editing values can only playing.", "Close");

                GUI.FocusControl(string.Empty);

                return;
            }

            if (!EnableEdit)
            {
                EditorUtility.DisplayDialog("Require unlock", "Editing values is locked.\nUnlock if you want to change the value.", "Close");

                GUI.FocusControl(string.Empty);

                return;
            }

            if (GetValue(record, valueName).Equals(value)) { return; }

            var originData = changedRecords.GetValueOrDefault(record);

            var valueNames = GetValueNames();

            if (originData == null)
            {
                var recordType = record.GetType();

                var args = valueNames.Select(t => GetValue(record, t)).ToArray();

                originData = Activator.CreateInstance(recordType, args);

                changedRecords.Add(record, originData);
            }

            SetValue(record, valueName, value);

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

        public bool IsChanged(object record, string valueName)
        {
            var originData = changedRecords.GetValueOrDefault(record);

            if (originData == null) { return false; }

            var valueType = GetValueType(valueName);

            var originValue = GetValue(originData, valueName);
            var currentValue = GetValue(record, valueName);

            if (valueType.IsArray)
            {
                var originArray = ((Array)originValue).Cast<object>().ToArray();
                var currentArray = ((Array)currentValue).Cast<object>().ToArray();

                if (originArray.Length != currentArray.Length) { return true; }

                for (var i = 0; i < originArray.Length; i++)
                {
                    if (!originArray[i].Equals(currentArray[i])) { return true; }
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

            return !originValue.Equals(currentValue);
        }

        /// <summary> マスター表示名取得. </summary>
        public abstract string GetDisplayMasterName();

        /// <summary> 値名取得. </summary>
        public abstract string[] GetValueNames();

        /// <summary> 値の型取得. </summary>
        public abstract Type GetValueType(string valueName);

        /// <summary> 値の設定. </summary>
        public abstract void SetValue(object record, string valueName, object value);

        /// <summary> 値の取得. </summary>
        public abstract object GetValue(object record, string valueName);
    }
}
