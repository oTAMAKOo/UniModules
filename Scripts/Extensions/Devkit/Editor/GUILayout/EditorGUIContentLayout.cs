
using UnityEngine;
using UnityEditor;

namespace Extensions.Devkit
{
    public static class EditorGUIContentLayout
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        //----- method -----

        public static void BeginContents()
        {
			GUILayout.Space(-2f);

            EditorGUILayout.BeginHorizontal(GUI.skin.box);

            EditorGUILayout.BeginVertical();

            GUILayout.Space(2f);
        }

        public static void EndContents()
        {
            GUILayout.Space(2f);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

    }
}
