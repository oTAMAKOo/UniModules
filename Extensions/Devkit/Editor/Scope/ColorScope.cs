
using UnityEngine;

namespace Extensions.Devkit
{
    public sealed class ColorScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        private readonly Color originColor;

        //----- property -----

        //----- method -----

        public ColorScope(Color color)
        {
            originColor = GUI.color;

            GUI.color = color;
        }

        protected override void CloseScope()
        {
            GUI.color = originColor;
        }
    }
}
