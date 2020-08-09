
using UnityEngine;

namespace Extensions.Devkit
{
    public sealed class ContentColorScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        private readonly Color originColor;

        //----- property -----

        //----- method -----

        public ContentColorScope(Color color)
        {
            originColor = GUI.contentColor;

            GUI.contentColor = color;
        }

        protected override void CloseScope()
        {
            GUI.contentColor = originColor;
        }
    }
}
