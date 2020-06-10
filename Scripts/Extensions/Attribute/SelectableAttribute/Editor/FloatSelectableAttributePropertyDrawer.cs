
using UnityEditor;

namespace Extensions
{
    [CustomPropertyDrawer(typeof(FloatSelectableAttribute))]
    public sealed class FloatSelectableAttributePropertyDrawer : SelectableAttributePropertyDrawer<float>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        protected override float[] Values
        {
            get { return ((FloatSelectableAttribute)attribute).Values; }
        }

        //----- method -----

        protected override float GetValue(SerializedProperty property)
        {
            return property.floatValue;
        }

        protected override void SetValue(SerializedProperty property, float value)
        {
            property.floatValue = value;
        }
    }
}
