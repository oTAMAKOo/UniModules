
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Extensions
{
    public abstract class SelectableAttributePropertyDrawer<T> : PropertyDrawer
    {
		//----- params -----

		//----- field -----

        //----- property -----

        protected abstract T[] Values { get; }

        //----- method -----

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var values = Values;

            if (values == null || values.IsEmpty())
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                var current = GetValue(property);
                var index = values.IndexOf(x => x.Equals(current));

                if (index < 0)
                {
                    index = 0;
                }

                var labels = GetLabels(values);

                index = EditorGUI.Popup(position, label.text, index, labels);

                SetValue(property, values[index]);
            }
        }

        private string[] GetLabels(T[] array)
        {
            return array.Select(x => x.ToString()).ToArray();
        }

        protected abstract T GetValue(SerializedProperty property);
        protected abstract void SetValue(SerializedProperty property, T value);
    }
}
