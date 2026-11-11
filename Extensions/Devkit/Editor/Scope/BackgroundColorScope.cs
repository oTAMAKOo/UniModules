
using UnityEngine;

namespace Extensions.Devkit
{
    public sealed class BackgroundColorScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        private readonly Color originColor;

        //----- property -----

        //----- method -----

        public BackgroundColorScope(Color color)
        {
            originColor = GUI.backgroundColor;

            GUI.backgroundColor = color;
        }

        protected override void CloseScope()
        {
            GUI.backgroundColor = originColor;
        }
    }
}
