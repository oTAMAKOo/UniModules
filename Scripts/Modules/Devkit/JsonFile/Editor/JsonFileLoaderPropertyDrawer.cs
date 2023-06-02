
using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.JsonFile
{
    [CustomPropertyDrawer(typeof(JsonFileLoader))]
    public sealed class JsonFileLoaderPropertyDrawer : PropertyDrawer
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        //----- method -----

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            const float EditButtonWidth = 60f;
            const float Space = 2f;
            
            var serializedObject = property.serializedObject;

            var jsonFileRelativePathProperty = property.FindPropertyRelative("jsonFileRelativePath");
            
            position = EditorGUI.PrefixLabel(position,  GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Json File Relative Path"));

            position.width -= EditButtonWidth + Space;

            jsonFileRelativePathProperty.stringValue = EditorGUI.TextField(position, jsonFileRelativePathProperty.stringValue);

            position.x += position.width + Space;
            position.width = EditButtonWidth;

            if (GUI.Button(position, "edit", EditorStyles.miniButton))
            {
                EditorApplication.delayCall += () =>
                {
                    var projectFolderPath = UnityPathUtility.GetProjectFolderPath();

                    var jsonFilePath = EditorUtility.OpenFilePanel("Select Json", projectFolderPath, "json");

                    if (!string.IsNullOrEmpty(jsonFilePath))
                    {
                        var assetFolderUri = new Uri(Application.dataPath);
                        var targetUri = new Uri(jsonFilePath);

                        jsonFileRelativePathProperty.stringValue = assetFolderUri.MakeRelativeUri(targetUri).ToString();

                        serializedObject.ApplyModifiedProperties();
                    }
                };
            }
        }
    }
}