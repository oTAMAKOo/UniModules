
using UnityEngine;
using UnityEditor;

namespace Extensions.Devkit
{
    public sealed class IndentLevelScope : GUI.Scope
    {
        //----- params -----

        //----- field -----

        private int originIndentLevel = 0;

        //----- property -----

        //----- method -----

        public IndentLevelScope(int indentLevel)
        {
            originIndentLevel = EditorGUI.indentLevel;

            EditorGUI.indentLevel = indentLevel;
        }

        protected override void CloseScope()
        {
            EditorGUI.indentLevel = originIndentLevel;
        }
    }
}
