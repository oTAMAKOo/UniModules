
using UnityEngine;
using UnityEditor;

namespace Extensions.Devkit
{
    public sealed class LabelWidthScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        private readonly float originLabelWidth = 0f;

        //----- property -----

        //----- method -----

        public LabelWidthScope(float labelWidth)
        {
            originLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = labelWidth;
        }

        protected override void CloseScope()
        {
            EditorGUIUtility.labelWidth = originLabelWidth;
        }
    }
}
