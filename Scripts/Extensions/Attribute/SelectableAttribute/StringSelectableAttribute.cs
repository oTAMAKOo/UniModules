
using UnityEngine;

namespace Extensions
{
	public class StringSelectableAttribute : PropertyAttribute
    {
        //----- params -----

        //----- field -----

        private string[] values = new string[0];

        //----- property -----

        public string[] Values { get { return values; } }

        //----- method -----

        public StringSelectableAttribute(params string[] args)
        {
            values = args;
        }
    }
}