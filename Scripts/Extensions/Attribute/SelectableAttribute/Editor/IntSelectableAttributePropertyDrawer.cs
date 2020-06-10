
using UnityEditor;

namespace Extensions
{
    [CustomPropertyDrawer(typeof(IntSelectableAttribute))]
    public sealed class IntSelectableAttributePropertyDrawer : SelectableAttributePropertyDrawer<int>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        protected override int[] Values
        {
            get { return ((IntSelectableAttribute)attribute).Values; }
        }

        //----- method -----

        protected override int GetValue(SerializedProperty property)
        {
            return property.intValue;
        }

        protected override void SetValue(SerializedProperty property, int value)
        {
            property.intValue = value;
        }
    }
}
