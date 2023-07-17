
using UnityEngine;
using UnityEditor;
using System.IO;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;

namespace Modules.Devkit.Inspector
{
    public sealed class FolderInspector : ExtendInspector
    {
        //----- params -----

        //----- field -----

        private string folderAssetPath = null;
        
        private bool edit = false;

        private string description = null;

        private static AesCryptoKey cryptoKey = null;

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
            EditorGUILayout.Space(1f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(edit ? "Save" : "Edit", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    if (edit)
                    {
                        SaveDescription();
                    }

                    edit = !edit;
                }
            }

            var size = EditorStyles.textArea.CalcSize(new GUIContent(description));

            var height = size.y < 80f ? 80f : size.y;

            if (edit)
            {
                using (new BackgroundColorScope(edit ? Color.gray : GUI.backgroundColor))
                {
                    EditorGUI.BeginChangeCheck();
            
                    var value = EditorGUILayout.TextArea(description, GUILayout.Height(height));

                    if(EditorGUI.EndChangeCheck())
                    {
                        description = value;
                    }
                }
            }
            else
            {
                EditorGUILayout.SelectableLabel(description, EditorStyles.textArea, GUILayout.Height(height));
            }
        }

        public override void OnEnable(UnityEngine.Object target)
        {
            folderAssetPath = AssetDatabase.GetAssetPath(target);

            var assetImporter = AssetImporter.GetAtPath(folderAssetPath);

            if (cryptoKey == null)
            {
                cryptoKey = ProjectCryptoKey.Instance.GetCryptoKey();
            }

            description = assetImporter.userData.Decrypt(cryptoKey);

            RepaintInspector();
        }

        private void SaveDescription()
        {
            var assetImporter = AssetImporter.GetAtPath(folderAssetPath);

            if (assetImporter == null){ return; }

            var selectObject = Selection.activeObject;
            
            var metaFilePath = Path.ChangeExtension(assetImporter.assetPath, ".meta");

            if (!File.Exists(metaFilePath)){ return; }

            if (cryptoKey == null)
            {
                cryptoKey = ProjectCryptoKey.Instance.GetCryptoKey();
            }

            var cryptoText = description.Encrypt(cryptoKey);

            if (cryptoText == null)
            {
                cryptoText = string.Empty;
            }

            if (assetImporter.userData != cryptoText)
            {
                assetImporter.userData = cryptoText;

                assetImporter.SaveAndReimport();
            }

            Selection.activeObject = selectObject;
        }

        private static void RepaintInspector()
        {
            var window = Resources.FindObjectsOfTypeAll<EditorWindow>();

            var inspectorWindow = ArrayUtility.FindAll(window, c => c.GetType().Name == "InspectorWindow").ToArray();

            inspectorWindow.ForEach(x => x.Repaint());
        }
    }
}
