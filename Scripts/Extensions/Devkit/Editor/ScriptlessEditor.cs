
using UnityEditor;

namespace Extensions.Devkit
{
    public abstract class ScriptlessEditor : Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        protected void DrawDefaultScriptlessInspector()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
