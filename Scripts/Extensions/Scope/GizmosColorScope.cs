
using UnityEngine;

namespace Extensions
{
    public sealed class GizmosColorScope : Scope
    {
        //----- params -----

        //----- field -----

        private Color originColor = Color.clear;

        //----- property -----

        //----- method -----

        public GizmosColorScope(Color color)
        {
            originColor = Gizmos.color;

            Gizmos.color = color;
        }

        protected override void CloseScope()
        {
            Gizmos.color = originColor;
        }
    }
}