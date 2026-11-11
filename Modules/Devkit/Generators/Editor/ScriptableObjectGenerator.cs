
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Generators
{
    public sealed class ScriptableObjectGenerator : UnityEditor.Editor
    {
        //------ params ------

        private const string AssetFileExtension = "asset";
        private const string DefaultAssetPath = "Assets/New ScriptableObject";

        //------ fields ------

        //------ property ------

        //------ methods ------

        public static bool Generate()
        {
            var directory = string.Empty;
            var assetPath = string.Empty;

            var selectionObject = Selection.activeObject;

            if (selectionObject != null)
            {
                var path = AssetDatabase.GetAssetPath(selectionObject);

                directory = AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path);

                var assetName = Path.GetFileName(DefaultAssetPath);

                assetPath = PathUtility.Combine(directory, assetName);
            }
            else
            {
                assetPath = DefaultAssetPath;
            }

            var instance = GenerateScriptableObject(typeof(ScriptableObject), assetPath);

            UnityEditorUtility.SelectAsset(instance);

            return instance;
        }
        
        public static Object Generate(Type type, string assetPath = null, bool log = false)
        {
            Object instance = null;

            if (!type.IsSubclassOf(typeof(ScriptableObject)))
            {
                Debug.LogErrorFormat("Generation failed require subclass of ScriptableObject.\n{0}", type.FullName);

                return null;
            }

            var path = string.IsNullOrEmpty(assetPath) ? DefaultAssetPath : assetPath;

            instance = GenerateScriptableObject(type, path);
            
            if (instance != null && log)
            {
                using (new DisableStackTraceScope())
                {
                    Debug.LogFormat("Generate : {0}", path);
                }
            }

            return instance;
        }

        public static T Generate<T>(string assetPath = null, bool log = true) where T : ScriptableObject
        {
            return Generate(typeof(T), assetPath, log) as T;
        }

        private static Object GenerateScriptableObject(Type type, string assetPath)
        {
            assetPath = Path.ChangeExtension(assetPath, AssetFileExtension);

            var instance = AssetDatabase.LoadAssetAtPath(assetPath, type);

            if (instance == null)
            {
                var path = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

                if (!File.Exists(path))
                {
                    instance = CreateInstance(type);

                    var directory = Path.GetDirectoryName(path);

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    AssetDatabase.CreateAsset(instance, assetPath);

                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            return instance;
        }
    }
}
