
using UnityEngine;
using UnityEditor;
using System.IO;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Crypto
{
    public sealed class KeyFileWindow : SingletonEditorWindow<KeyFileWindow>
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(450f, 140f);

        public static class Prefs
        {
            public static string FileDirectory
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-FileDirectory"); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-FileDirectory", value); }
            }
        }

        //----- field -----

        private string encryptKey = null;
        private string encryptIv = null;

        private string projectFolderPath = null;

        private IKeyFileManager keyFileManager = null;

        //----- property -----

        //----- method -----

        public static void Open(IKeyFileManager keyFileManager)
        {
            Instance.maxSize = WindowSize;
            Instance.minSize = WindowSize;

            Instance.titleContent = new GUIContent("Generate KeyFile");

            Instance.keyFileManager = keyFileManager;

            Instance.ShowUtility();
        }

        void OnGUI()
        {
            EditorLayoutTools.Title("Generate KeyFile");

            if (string.IsNullOrEmpty(projectFolderPath))
            {
                projectFolderPath = UnityPathUtility.GetProjectFolderPath();
            }

            using (new LabelWidthScope(50f))
            {
                GUILayout.Space(2f);

                EditorGUI.BeginChangeCheck();

                var key = EditorGUILayout.TextField("Key", encryptKey);

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

                var iv = EditorGUILayout.TextField("Iv", encryptIv);

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
                        var fileDirectory = Prefs.FileDirectory;

                        if (!Directory.Exists(fileDirectory))
                        {
                            fileDirectory = projectFolderPath;
                        }

                        var filePath = EditorUtility.SaveFilePanel("Generate KeyFile", fileDirectory, "keyfile", string.Empty);

                        if (!filePath.IsNullOrEmpty())
                        {
                            keyFileManager.Create(filePath, encryptKey, encryptIv);

                            var keyFile = keyFileManager.Load(filePath);

                            if (keyFile != null)
                            {
                                using (new DisableStackTraceScope())
                                {
                                    Debug.LogFormat("Generate success.\n\nFilePath: {0}\n\nKey: {1}\nIv: {2}", filePath, keyFile.Key, keyFile.Iv);
                                }

                                var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(filePath);

                                AssetDatabase.ImportAsset(assetPath);

                                Prefs.FileDirectory = Path.GetDirectoryName(filePath);
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
                    var fileDirectory = Prefs.FileDirectory;

                    if (!Directory.Exists(fileDirectory))
                    {
                        fileDirectory = projectFolderPath;
                    }

                    var filePath = EditorUtility.OpenFilePanel("Load KeyFile", fileDirectory, string.Empty);

                    if (!filePath.IsNullOrEmpty())
                    {
                        var keyFile = keyFileManager.Load(filePath);

                        using (new DisableStackTraceScope())
                        {
                            Debug.LogFormat("FilePath: {0}\nKey: {1}\nIv: {2}", filePath, keyFile.Key, keyFile.Iv);
                        }

                        Prefs.FileDirectory = Path.GetDirectoryName(filePath);
                    }
                }

                GUILayout.Space(5f);
            }
        }
    }
}
