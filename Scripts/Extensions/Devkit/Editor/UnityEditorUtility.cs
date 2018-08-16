
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Extensions;

using Object = UnityEngine.Object;

namespace Extensions.Devkit
{
    public static class UnityEditorUtility
    {
        public const string AssetsFolderName = "Assets/";

        public const string MetaFileExtension = ".meta";

        /// <summary>
        /// 編集履歴に登録.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="objects"></param>
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
            var gameObjects = rootObjects.SelectMany(x => x.DescendantsAndSelf());

            return inactive ? gameObjects.ToArray() : gameObjects.Where(x => x.activeSelf).ToArray();
        }

        #region Prefab

        /// <summary>
        /// Prefabか判定.
        /// </summary>
        public static bool IsPrefab(Object instance)
        {
            return PrefabUtility.GetPrefabParent(instance) == null && PrefabUtility.GetPrefabObject(instance) != null;
        }

        /// <summary>
        /// Prefabから生成されたインスタンスか判定.
        /// </summary>
        public static bool IsPrefabInstance(Object instance)
        {
            return PrefabUtility.GetPrefabParent(instance) != null && PrefabUtility.GetPrefabObject(instance) != null;
        }

        /// <summary>
        /// Prefabに変更がある場合適用.
        /// </summary>
        public static bool ApplyPrefabIfModifications(Object instance)
        {
            var go = instance as GameObject;

            if (go == null) { return false; }

            // Prefabのインスタンスかどうかをチェック.
            if (!IsPrefabInstance(go)) { return false; }

            // 変更がある場合のみ置き換える.
            if (PrefabUtility.GetPropertyModifications(go).Length <= 0) { return false; }

            PrefabUtility.ReplacePrefab(go, PrefabUtility.GetPrefabParent(go));

            return true;
        }

        #endregion

        #region Asset

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

        /// <summary>
        /// 指定されたアセットを保存.
        /// </summary>
        /// <param name="asset"></param>
        public static void SaveAsset(Object asset)
        {
            if (AssetDatabase.IsMainAsset(asset) || AssetDatabase.IsSubAsset(asset))
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError("This asset is not project asset.");
            }
        }

        /// <summary>
        /// フォルダ判定.
        /// </summary>
        /// <param name="assetObject"></param>
        /// <returns></returns>
        public static bool IsFolder(UnityEngine.Object assetObject)
        {
            var path = AssetDatabase.GetAssetPath(assetObject);

            if (File.Exists(path))
            {
                return false;
            }

            if (Directory.Exists(path))
            {
                return true;
            }

            return false;
        }

        public static bool IsFolder(string path)
        {
            if (File.Exists(path))
            {
                return false;
            }

            if (Directory.Exists(path))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 指定Assetのフルパスを取得.
        /// </summary>
        /// <param name="assetObject"></param>
        /// <returns></returns>
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

        /// <summary>
        /// フォルダ内の全Assetを取得.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="folderPath"></param>
        /// <returns></returns>
        public static T[] LoadAssetsInFolder<T>(string folderPath) where T : UnityEngine.Object
        {
            var assets = new List<T>();

            if (folderPath.StartsWith(AssetsFolderName))
            {
                folderPath = folderPath.Remove(0, AssetsFolderName.Length);
            }

            var dir = PathUtility.Combine(Application.dataPath, folderPath);

            if (!Directory.Exists(dir))
            {
                Debug.LogFormat("指定されたディレクトリが存在しません.\n{0}", dir);
                return new T[0];
            }

            var queue = new Queue();

            queue.Enqueue(dir);

            while (queue.Count > 0)
            {
                var d = (string)queue.Dequeue();

                var files = Directory.GetFiles(d);
                foreach (var file in files)
                {
                    var path = file.Replace(Application.dataPath, "Assets");
                    var asset = AssetDatabase.LoadAssetAtPath<T>(path);

                    if (asset != null)
                    {
                        assets.Add(asset);
                    }
                }

                var dirs = Directory.GetDirectories(d);
                foreach (var s in dirs)
                {
                    queue.Enqueue(s);
                }
            }

            return assets.ToArray();
        }

        /// <summary> 型でアセットを検索 </summary>
        public static T[] FindAssetsByType<T>() where T : UnityEngine.Object
        {
            var assets = new List<T>();

            var guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));

            for (var i = 0; i < guids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets.ToArray();
        }

        /// <summary>
        /// GUIDからAssetを取得.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static Object FindMainAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid)) { return null; }

            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            return AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        /// <summary>
        /// AssetからGUIDを取得.
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public static string GetAssetGUID(Object asset)
        {
            if (asset == null) { return null; }

            if (!AssetDatabase.IsMainAsset(asset)) { return null; }

            var assetPath = AssetDatabase.GetAssetPath(asset);

            return AssetDatabase.AssetPathToGUID(assetPath);
        }

        private const string EditingAssetTag = "Edit";

        /// <summary>
        /// アセットに編集状態のタグを付与.
        /// </summary>
        public static void RegisterEditAsset(string assetPath)
        {
            var assetImporter = AssetImporter.GetAtPath(assetPath);

            if (!assetImporter.userData.Contains(EditingAssetTag))
            {
                assetImporter.userData += EditingAssetTag;

                assetImporter.SaveAndReimport();
            }
        }

        /// <summary>
        /// アセットに編集状態のタグを付与.
        /// </summary>
        public static void RegisterEditAsset(Object assetObject)
        {
            RegisterEditAsset(AssetDatabase.GetAssetPath(assetObject));
        }

        /// <summary>
        /// アセットの編集を終了し編集状態のタグを削除.
        /// </summary>
        public static void ReleaseEditAsset(string assetPath)
        {
            var assetImporter = AssetImporter.GetAtPath(assetPath);

            if (assetImporter == null) { return; }

            if (assetImporter.userData.Contains(EditingAssetTag))
            {
                assetImporter.userData = assetImporter.userData.Replace(EditingAssetTag, string.Empty);

                assetImporter.SaveAndReimport();
            }
        }

        /// <summary>
        /// アセットの編集を終了し編集状態のタグを削除.
        /// </summary>
        public static void ReleaseEditAsset(Object assetObject)
        {
            ReleaseEditAsset(AssetDatabase.GetAssetPath(assetObject));
        }

        /// <summary>
        /// アセットが編集中状態か取得.
        /// </summary>
        public static bool IsEditAsset(string assetPath)
        {
            var assetImporter = AssetImporter.GetAtPath(assetPath);

            return assetImporter != null && assetImporter.userData.Contains(EditingAssetTag);
        }

        /// <summary>
        /// アセットが編集中状態か取得.
        /// </summary>
        public static bool IsEditAsset(Object assetObject)
        {
            return IsEditAsset(AssetDatabase.GetAssetPath(assetObject));
        }

        #endregion


        #region Audio

        public static void PlayClip(AudioClip clip, float startTime = 0f, bool loop = false)
        {
            int startSample = (int)(startTime * clip.frequency);

            Assembly assembly = typeof(AudioImporter).Assembly;
            Type audioUtilType = assembly.GetType("UnityEditor.AudioUtil");

            Type[] typeParams = { typeof(AudioClip), typeof(int), typeof(bool) };
            object[] objParams = { clip, startSample, loop };

            MethodInfo method = audioUtilType.GetMethod("PlayClip", typeParams);
            method.Invoke(null, BindingFlags.Static | BindingFlags.Public, null, objParams, null);
        }

        public static void StopAllClips()
        {
            var unityEditorAssembly = typeof(AudioImporter).Assembly;
            var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllClips",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new System.Type[] { },
                null
            );
            method.Invoke(null, new object[] { });
        }

        #endregion
    }
}