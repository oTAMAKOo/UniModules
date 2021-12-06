
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.Crypto
{
    public sealed class KeyFileWinodow : SingletonEditorWindow<KeyFileWinodow>
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(450f, 140f);

        //----- field -----

        private string encryptKey = null;
        private string encryptIv = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.maxSize = WindowSize;
            Instance.minSize = WindowSize;

            Instance.titleContent = new GUIContent("Generate KeyFile");

            Instance.ShowUtility();
        }

        void OnGUI()
        {
            EditorLayoutTools.Title("Generate KeyFile");

            using (new LabelWidthScope(50f))
            {
                GUILayout.Space(2f);

                EditorGUI.BeginChangeCheck();

                var key = EditorGUILayout.DelayedTextField("Key", encryptKey);

                if (EditorGUI.EndChangeCheck())
                {
                    if (!key.IsNullOrEmpty())
                    {
                        if (key.Length == 32)
                        {
                            encryptKey = key;
                        }
                        else
                        {
                            Debug.LogError("Key must be 32 characters");
                        }
                    }
                }

                GUILayout.Space(2f);
                
                EditorGUI.BeginChangeCheck();

                var iv = EditorGUILayout.DelayedTextField("Iv", encryptIv);

                if (EditorGUI.EndChangeCheck())
                {
                    if (!iv.IsNullOrEmpty())
                    {
                        if (iv.Length == 16)
                        {
                            encryptIv = iv;
                        }
                        else
                        {
                            Debug.LogError("Iv must be 16 characters");
                        }
                    }
                }
            }

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new DisableScope(encryptKey.IsNullOrEmpty() || encryptIv.IsNullOrEmpty()))
                {
                    if (GUILayout.Button("Generate", EditorStyles.miniButton, GUILayout.Width(250f)))
                    {
                        var path = EditorUtility.SaveFilePanelInProject("Generate KeyFile", "keyfile", string.Empty, "Select key file path.");

                        var filePath = UnityPathUtility.ConvertAssetPathToFullPath(path);

                        KeyFile.Create(filePath, encryptKey, encryptIv);

                        var keyFile = KeyFile.Load(filePath);

                        if (keyFile != null)
                        {
                            using (new DisableStackTraceScope())
                            {
                                Debug.LogFormat("Generate success.\n\nFilePath: {0}\n\nKey: {1}\nIv: {2}", filePath, keyFile.Item1, keyFile.Item2);
                            }
                        }
                    }
                }

                GUILayout.Space(5f);
            }
            
            GUILayout.Space(4f);

            EditorLayoutTools.Title("Check KeyFile");

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Load", EditorStyles.miniButton, GUILayout.Width(250f)))
                {
                    var projectFolderPath = UnityPathUtility.GetProjectFolderPath();

                    var filePath = EditorUtility.OpenFilePanel("Load KeyFile", projectFolderPath, string.Empty);

                    if (!filePath.IsNullOrEmpty())
                    {
                        var keyFile = KeyFile.Load(filePath);

                        using (new DisableStackTraceScope())
                        {
                            Debug.LogFormat("Key: {0}\nIv: {1}", keyFile.Item1, keyFile.Item2);
                        }
                    }
                }

                GUILayout.Space(5f);
            }
        }
    }
}
