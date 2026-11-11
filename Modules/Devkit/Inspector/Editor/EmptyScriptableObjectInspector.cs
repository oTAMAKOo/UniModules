
using UnityEngine;
using UnityEditor;

namespace Modules.Devkit.Inspector
{
    [CustomEditor(typeof(ScriptableObject))]
    public sealed class EmptyScriptableObjectInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var scriptProperty = serializedObject.FindProperty("m_Script");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(scriptProperty);

            if (EditorGUI.EndChangeCheck())
            {
                var script = scriptProperty.objectReferenceValue as MonoScript;

                if (script != null)
                {
                    var scriptClass = script.GetClass();

                    var isScriptableObject = scriptClass.IsSubclassOf(typeof(ScriptableObject));

                    if (!isScriptableObject)
                    {
                        scriptProperty.objectReferenceValue = null;

                        Debug.LogError($"Specified class is not a ScriptableObject.\nclass : {scriptClass.FullName}");
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
