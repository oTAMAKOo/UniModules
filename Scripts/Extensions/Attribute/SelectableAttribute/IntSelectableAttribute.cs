
using UnityEngine;

namespace Extensions
{
	public sealed class IntSelectableAttribute : PropertyAttribute
    {
        //----- params -----

        //----- field -----

        private int[] values = new int[0];

        //----- property -----

        public int[] Values { get { return values; } }

        //----- method -----

        public IntSelectableAttribute(params int[] args)
        {
            values = args;
        }
    }
}
