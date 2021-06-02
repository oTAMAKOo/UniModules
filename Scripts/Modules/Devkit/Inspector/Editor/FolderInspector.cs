
using UnityEngine;
using UnityEditor;
using Extensions;

namespace Modules.Devkit.Inspector
{
    public sealed class FolderInspector : ExtendInspector
    {
        //----- params -----

        //----- field -----

        private string folderAssetPath = null;
        
        private int lineCount = 0;

        private string description = null;

        //----- property -----

        public override int Priority { get { return 0; } }

        //----- method -----

        public override bool Validation(UnityEngine.Object target)
        {
            var folderAssetPath = AssetDatabase.GetAssetPath(target);

            return AssetDatabase.IsValidFolder(folderAssetPath);
        }

        public override void DrawInspectorGUI(UnityEngine.Object target)
        {
            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(20f)))
            {
                if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    EditorUtility.RevealInFinder(folderAssetPath);
                }

                if (GUILayout.Button("Copy Path", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    EditorGUIUtility.systemCopyBuffer = folderAssetPath;
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(2f);

            var height = lineCount < 4 ? 80f : 0f;
            
            EditorGUI.BeginChangeCheck();

            description = EditorGUILayout.TextArea(description, EditorStyles.textArea, GUILayout.Height(height));

            if(EditorGUI.EndChangeCheck())
            {
                UpdateLineCount();
            }
        }

        public override void OnEnable(UnityEngine.Object target)
        {
            folderAssetPath = AssetDatabase.GetAssetPath(target);

            var assetImporter = AssetImporter.GetAtPath(folderAssetPath);

            description = assetImporter.userData;

            UpdateLineCount();

            RepaintInspector();
        }

        public override void OnDisable(UnityEngine.Object target)
        {
            var assetImporter = AssetImporter.GetAtPath(folderAssetPath);

            if (assetImporter != null)
            {
                if (assetImporter.userData != description)
                {
                    assetImporter.userData = description;

                    assetImporter.SaveAndReimport();
                }
            }
        }

        public override void OnDestroy(UnityEngine.Object target) { }

        private void UpdateLineCount()
        {
            lineCount = description.Split('\n').Length;
        }

        private static void RepaintInspector()
        {
            var window = Resources.FindObjectsOfTypeAll<EditorWindow>();

            var inspectorWindow = ArrayUtility.FindAll(window, c => c.GetType().Name == "InspectorWindow").ToArray();

            inspectorWindow.ForEach(x => x.Repaint());
        }
    }
}
