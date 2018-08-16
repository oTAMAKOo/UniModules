
using UnityEditor;

namespace Extensions
{
    [CustomPropertyDrawer(typeof(StringSelectableAttribute))]
    public class StringSelectableAttributePropertyDrawer : SelectableAttributePropertyDrawer<string>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        protected override string[] Values
        {
            get { return ((StringSelectableAttribute)attribute).Values; }
        }

        //----- method -----

        protected override string GetValue(SerializedProperty property)
        {
            return property.stringValue;
        }

        protected override void SetValue(SerializedProperty property, string value)
        {
            property.stringValue = value;
        }
    }
}