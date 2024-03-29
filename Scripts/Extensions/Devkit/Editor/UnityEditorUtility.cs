
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Object = UnityEngine.Object;

namespace Extensions.Devkit
{
    public static class UnityEditorUtility
    {
        public const string AssetsFolderName = "Assets/";

        public const string MetaFileExtension = ".meta";

        /// <summary> 編集履歴に登録. </summary>
        public static void RegisterUndo<T>(T target) where T : Object
        {
            RegisterUndo(typeof(T).FullName + " Undo", target);
        }

        /// <summary> 編集履歴に登録. </summary>
        public static void RegisterUndo(string name, params Object[] objects)
        {
            if (objects != null && objects.Length > 0)
            {
                Undo.RecordObjects(objects, name);

                foreach (Object obj in objects)
                {
                    if (obj == null) continue;
                    EditorUtility.SetDirty(obj);
                }
            }
        }

        /// <summary>
        /// 現在のシーンのヒエラルキーのルートオブジェクトを取得.
        /// </summary>
        /// <param name="inactive"></param>
        /// <returns></returns>
        public static GameObject[] FindRootObjectsInHierarchy(bool inactive = true)
        {
            var currentScene = EditorSceneManager.GetSceneAt(0);

            var rootObjects = currentScene.GetRootGameObjects();

            return inactive ? rootObjects.ToArray() : rootObjects.Where(x => x.activeSelf).ToArray();
        }

        /// <summary>
        /// 現在のシーンのヒエラルキーの全オブジェクトを取得.
        /// </summary>
        /// <param name="inactive"></param>
        /// <returns></returns>
        public static GameObject[] FindAllObjectsInHierarchy(bool inactive = true)
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            var rootObjects = currentScene.GetRootGameObjects();
            var gameObjects = rootObjects.SelectMany(x => UnityUtility.GetChildrenAndSelf(x));

            return inactive ? gameObjects.ToArray() : gameObjects.Where(x => x.activeSelf).ToArray();
        }

        /// <summary>
        /// Missing状態のComponent or Fieldがあるか取得.
        /// </summary>
        public static bool HasMissingReference(GameObject gameObject)
        {
            var components = UnityUtility.GetComponents<Component>(gameObject).ToArray();

            // コンポーネントのスクリプトがMissing.
            
            if (components.Any(x => x == null)){ return true; }

            // コンポーネントのSerializeFieldがMissing.
            
            foreach (var component in components)
            {
                var so = new SerializedObject(component);

                var sp = so.GetIterator();

                while (sp.NextVisible(true))
                {
                    if (sp.propertyType != SerializedPropertyType.ObjectReference){ continue; }

                    if (sp.objectReferenceValue != null ){ continue; }

                    if (!sp.hasChildren ){ continue; }

                    var fileId = sp.FindPropertyRelative( "m_FileID" );
                    
                    if (fileId == null){ continue; }
                    
                    if (fileId.intValue == 0) { continue; }

                    return true;
                }
            }

            return false;
        }

        /// <summary> コンパイル実行. </summary>
        public static void RequestScriptCompilation()
        {
            #if UNITY_2021_2_OR_NEWER

            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);

            #elif UNITY_2019_3_OR_NEWER

            CompilationPipeline.RequestScriptCompilation();

            #endif
        }

        #region Prefab

        /// <summary> Prefabか判定 </summary>
        public static bool IsPrefab(Object instance)
        {
            return PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance) != null;
        }

        /// <summary> Prefabから生成されたインスタンスか判定 </summary>
        public static bool IsPrefabInstance(Object instance)
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(instance) != null && PrefabUtility.GetPrefabInstanceHandle(instance) != null;
        }

        #endregion

        #region Asset

        /// <summary> アセットが存在するか </summary>
        public static bool IsExists(string assetPath)
        {
            var path = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

            return File.Exists(path);
        }

        public static Object SelectAsset(string assetPath)
        {
            var instance = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

            return SelectAsset(instance);
        }

        public static Object SelectAsset(Object instance)
        {
            if (instance != null)
            {
                Selection.activeObject = instance;
            }

            return instance;
        }

        /// <summary> 指定されたアセットを保存 </summary>
        public static void SaveAsset(Object asset)
        {
            if (AssetDatabase.IsMainAsset(asset) || AssetDatabase.IsSubAsset(asset))
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }
            else
            {
                Debug.LogError("This asset is not project asset.");
            }
        }

        /// <summary> フォルダ判定 </summary>
        public static bool IsFolder(UnityEngine.Object assetObject)
        {
            var path = AssetDatabase.GetAssetPath(assetObject);
            
            return PathUtility.GetFilePathType(path) == PathUtility.FilePathType.Directory;
        }

        /// <summary> フォルダを開く </summary>
        public static void OpenFolder(string path)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                System.Diagnostics.Process.Start(path);
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                EditorUtility.RevealInFinder(path);
            }
        }

        /// <summary> 指定Assetのフルパスを取得 </summary>
        public static string GetAssetFullPath(UnityEngine.Object assetObject)
        {
            if (assetObject == null) { return null; }

            var path = AssetDatabase.GetAssetPath(assetObject);

            var result = UnityPathUtility.GetProjectFolderPath() + path;

            if (AssetDatabase.IsValidFolder(path))
            {
                result += PathUtility.PathSeparator;
            }

            return result;
        }

        /// <summary> フォルダ内の全AssetPathを取得 </summary>
        public static async Task<string[]> GetAllAssetPathInFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) { return new string[0]; }

            if (folderPath.StartsWith(AssetsFolderName))
            {
                folderPath = folderPath.Remove(0, AssetsFolderName.Length);
            }

            var dir = PathUtility.Combine(UnityPathUtility.DataPath, folderPath);

            var assetPaths = await DirectoryUtility.GetAllFilesAsync(dir);

            return assetPaths
                .Where(x => Path.GetExtension(x) != ".meta")
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .Select(x => x.Replace(UnityPathUtility.DataPath, UnityPathUtility.AssetsFolder))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
        }

        /// <summary> 型でアセットを検索 </summary>
        public static IEnumerable<T> FindAssetsByType<T>(string filter, string[] searchInFolders = null) where T : UnityEngine.Object
        {
            if (searchInFolders == null)
            {
                searchInFolders = new string[] { UnityPathUtility.AssetsFolder };
            }

            return AssetDatabase.FindAssets(filter, searchInFolders)
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadAssetAtPath<T>(x))
                .Where(x => x != null);
        }

        /// <summary> GUIDからAssetを取得 </summary>
        public static Object FindMainAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid)) { return null; }

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        /// <summary> AssetからGUIDを取得 </summary>
        public static string GetAssetGUID(Object asset)
        {
            if (asset == null) { return null; }

            if (!AssetDatabase.IsMainAsset(asset)) { return null; }

            var assetPath = AssetDatabase.GetAssetPath(asset);

            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        #endregion

        #region AssetBundle

        /// <summary> アセットバンドル名を取得. </summary>
        public static string GetAssetBundleName(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath);

            var assetBundleName = BuildAssetBundleName(importer);

            if (string.IsNullOrEmpty(assetBundleName))
            {
                var directory = Path.GetDirectoryName(assetPath);

                while (true)
                {
                    importer = AssetImporter.GetAtPath(directory);

                    if (importer == null) { break; }

                    assetBundleName = BuildAssetBundleName(importer);

                    if (!string.IsNullOrEmpty(assetBundleName)) { break; }

                    directory = Path.GetDirectoryName(directory);

                    if (string.IsNullOrEmpty(directory)) { break; }
                }
            }

            return assetBundleName;
        }

        private static string BuildAssetBundleName(AssetImporter importer)
        {
            var assetBundleName = string.Empty;

            var bundleName = importer.assetBundleName;
            var variantName = importer.assetBundleVariant;

            if (!string.IsNullOrEmpty(bundleName))
            {
                assetBundleName = string.IsNullOrEmpty(variantName) ? bundleName : bundleName + "." + variantName;
            }

            return assetBundleName;
        }

        #endregion

        #region Identifier

        private static PropertyInfo cachedInspectorModeInfo = null;

        /// <summary>
        /// ファイル内での識別子を取得.
        /// </summary>
        /// <returns>正常に取得できた時は1以上の値が返る. 失敗時・ファイルに保存されていないオブジェクトの場合は0が返る</returns>
        public static long GetLocalIdentifierInFile(Object unityObject)
        {
            long id = 0;

            if (unityObject == null) return id;

            if (cachedInspectorModeInfo == null)
            {
                cachedInspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var serializedObject = new SerializedObject(unityObject);

            cachedInspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

            var serializedProperty = serializedObject.FindProperty("m_LocalIdentfierInFile");

            return serializedProperty.longValue;
        }

        #endregion
    }
}
