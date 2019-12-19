
using UnityEngine;

namespace Extensions
{
	public sealed class FloatSelectableAttribute : PropertyAttribute
    {
        //----- params -----

        //----- field -----

        private float[] values = new float[0];

        //----- property -----

        public float[] Values { get { return values; } }

        //----- method -----

        public FloatSelectableAttribute(params float[] args)
        {
            values = args;
        }
    }
}
