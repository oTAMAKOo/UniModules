
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Generators
{
    public class ScriptableObjectGenerator : UnityEditor.Editor
    {
        //------ params ------

        private const string AssetFileExtension = "asset";
        private const string DefaultAssetPath = "Assets/ScriptableObject (Empty)";

        //------ fields ------

        //------ property ------

        //------ methods ------

        public static bool Generate()
        {
            var instance = Generate<ScriptableObject>();

            UnityEditorUtility.SelectAsset(instance);

            return instance;
        }

        public static Object Generate(Type type, string assetPath = null, bool log = true)
        {
            assetPath = Path.ChangeExtension(string.IsNullOrEmpty(assetPath) ? DefaultAssetPath : assetPath, AssetFileExtension);

            var instance = AssetDatabase.LoadAssetAtPath(assetPath, type);

            if (instance == null)
            {
                var projectPath = UnityPathUtility.ConvertProjectPath(assetPath);
                var path = PathUtility.Combine(Application.dataPath, projectPath);

                if (!File.Exists(path))
                {
                    instance = CreateInstance(type);
                    AssetDatabase.CreateAsset(instance, assetPath);
                    AssetDatabase.SaveAssets();

                    if (log)
                    {
                        Debug.Log(string.Format("Generate: {0}", assetPath));
                    }
                }
            }

            return instance;
        }

        public static T Generate<T>(string assetPath = null, bool log = true) where T : ScriptableObject
        {
            assetPath = Path.ChangeExtension(string.IsNullOrEmpty(assetPath) ? DefaultAssetPath : assetPath, AssetFileExtension);

            var instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (instance == null)
            {
                var projectPath = UnityPathUtility.ConvertProjectPath(assetPath);
                var path = PathUtility.Combine(Application.dataPath, projectPath);

                if (!File.Exists(path))
                {
                    AssetDatabase.CreateAsset(CreateInstance<T>(), assetPath);
                    AssetDatabase.SaveAssets();

                    instance = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                    if (log)
                    {
                        Debug.Log(string.Format("Generate: {0}", assetPath));
                    }
                }
            }

            return instance;
        }
    }
}