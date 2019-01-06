

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Linq;

namespace Extensions
{
    public static class UnityUtility
    {
        #region Object Instantiate

        private const string PrefabTag = " (Prefab)";

        /// <summary> 空のGameObjectを生成 </summary>
        public static GameObject CreateEmptyGameObject(GameObject parent, string name, bool worldPositionStays = false)
        {
            var gameObject = new GameObject();

            gameObject.transform.name = name;

            if (parent != null)
            {
                gameObject.transform.parent = parent.transform;

                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale = Vector3.one;

                SetLayer(parent, gameObject);
                SetParent(gameObject, parent, worldPositionStays);
            }

            return gameObject;
        }

        /// <summary> コンポーネント付きのGameObjectを生成 </summary>
        public static T CreateGameObject<T>(GameObject parent, string name, bool worldPositionStays = false)
            where T : Component
        {
            var gameObject = new GameObject();

            gameObject.transform.name = name;

            if (parent != null)
            {
                gameObject.transform.parent = parent.transform;

                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale = Vector3.one;

                SetLayer(parent, gameObject);
                SetParent(gameObject, parent, worldPositionStays);
            }

            return gameObject.AddComponent<T>();
        }

        /// <summary> 親オブジェクト取得 </summary>
        public static GameObject ParentGameObject(GameObject instance)
        {
            if (instance == null) { return null; }

            var parent = instance.transform.parent;

            return parent != null ? parent.gameObject : null;
        }

        /// <summary> パスからPrefab生成 </summary>
        public static GameObject Instantiate(GameObject parent, string path, bool instantiateInWorldSpace = false)
        {
            return Instantiate(parent, (GameObject)Resources.Load(path), instantiateInWorldSpace);
        }

        /// <summary> 参照オブジェクトからPrefab生成 </summary>
        public static GameObject Instantiate(GameObject parent, UnityEngine.Object prefab,
            bool instantiateInWorldSpace = false)
        {
            if (prefab != null)
            {
                GameObject gameObject = null;

                if (parent == null)
                {
                    gameObject = UnityEngine.Object.Instantiate(prefab) as GameObject;
                }
                else
                {
                    gameObject = UnityEngine.Object.Instantiate(prefab, parent.transform, instantiateInWorldSpace) as GameObject;
                }

                if (gameObject != null)
                {
                    gameObject.transform.name = prefab.name + PrefabTag;
                }

                return gameObject;
            }

            return null;
        }

        /// <summary> Resource パスから生成 + Component取得 </summary>
        public static T Instantiate<T>(GameObject parent, string path, bool instantiateInWorldSpace = false)
            where T : Component
        {
            var gameObject = Instantiate(parent, path, instantiateInWorldSpace);

            return GetComponent<T>(gameObject);
        }

        /// <summary> Prefab から生成 + Component取得 </summary>
        public static T Instantiate<T>(GameObject parent, UnityEngine.Object prefab,
            bool instantiateInWorldSpace = false) where T : Component
        {
            var gameObject = Instantiate(parent, prefab, instantiateInWorldSpace);

            return GetComponent<T>(gameObject);
        }

        /// <summary> Prefabから複数のインスタンスを高速生成 + Component取得 </summary>
        public static T[] Instantiate<T>(GameObject parent, UnityEngine.Object prefab, int count,
            bool instantiateInWorldSpace = false) where T : Component
        {
            var list = new List<T>();

            var instanceName = string.Empty;

            var sourceObject = prefab;

            for (var i = 0; i < count; i++)
            {
                var item = Instantiate<T>(parent, sourceObject, instantiateInWorldSpace);

                if (i == 0)
                {
                    sourceObject = item.gameObject;

                    instanceName = item.gameObject.transform.name;
                }
                else
                {
                    item.gameObject.transform.name = instanceName;
                }

                list.Add(item);
            }

            return list.ToArray();
        }

        #endregion

        #region Object Delete

        /// <summary> オブジェクトの削除 </summary>
        public static void SafeDelete(UnityEngine.Object instance, bool immediate = false)
        {
            if (instance != null)
            {
                var gameObject = instance as GameObject;

                if (gameObject != null)
                {
                    gameObject.SetActive(false);

                    if (!(gameObject.transform is RectTransform))
                    {
                        gameObject.transform.parent = null;
                    }
                }

                if (!Application.isPlaying || immediate)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
                else
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
        }

        /// <summary> コンポーネントの削除 </summary>
        public static void SafeDelete<T>(GameObject instance, bool immediate = false) where T : Component
        {
            var component = GetComponent<T>(instance);

            if (component != null)
            {
                SafeDelete(component, immediate);
            }
        }

        /// <summary> 複数オブジェクトの削除 </summary>
        public static void SafeDelete(List<GameObject> list)
        {
            foreach (var elem in list)
            {
                SafeDelete(elem);
            }

            list.Clear();
        }

        /// <summary> Nullチェック </summary>
        public static bool IsNull(object obj)
        {
            if (obj is UnityEngine.Object)
            {
                if ((UnityEngine.Object)obj != null)
                {
                    return false; // 元気なUnityオブジェクト.
                }
                else
                {
                    return true; // 死んだフリしているUnityオブジェクト.
                }
            }
            else
            {
                return obj == null;
            }
        }

        #endregion

        #region Active Control

        /// <summary> 状態取得 </summary>
        public static bool IsActive(GameObject instance)
        {
            return instance && instance.activeSelf;
        }

        /// <summary> 状態取得 </summary>
        public static bool IsActive<T>(T instance) where T : Component
        {
            return instance && instance.gameObject.activeSelf;
        }

        /// <summary> 階層内状態取得 </summary>
        public static bool IsActiveInHierarchy(GameObject instance)
        {
            return instance && instance.activeInHierarchy;
        }

        /// <summary> 階層内状態取得 </summary>
        public static bool IsActiveInHierarchy<T>(T instance) where T : Component
        {
            return instance && instance.gameObject.activeInHierarchy;
        }

        /// <summary> 状態設定 </summary>
        public static void SetActive(GameObject instance, bool state)
        {
            if (instance == null) { return; }

            if (state == instance.activeSelf) { return; }

            instance.SetActive(state);
        }

        /// <summary> 状態設定 </summary>
        public static void SetActive<T>(T instance, bool state) where T : Component
        {
            if (instance == null) { return; }

            if (state == instance.gameObject.activeSelf) { return; }

            instance.gameObject.SetActive(state);
        }

        #endregion

        #region Object Control

        /// <summary> 親オブジェクト設定 </summary>
        public static void SetParent(GameObject instance, GameObject parent, bool worldPositionStays = false)
        {
            if (instance != null)
            {
                instance.transform.SetParent(parent != null ? parent.transform : null, worldPositionStays);
            }
        }

        /// <summary> Hierarchy上の子オブジェクトのパス取得 </summary>
        /// <param name="root">基点オブジェクト</param>
        /// <param name="target">目標オブジェクト</param>
        /// <returns></returns>
        public static string GetChildHierarchyPath(GameObject root, GameObject target)
        {
            var path = string.Empty;

            var trans = target.transform.parent;

            while (trans != null && root != trans.gameObject)
            {
                path = PathUtility.Combine(trans.name, path);
                trans = trans.parent;
            }

            return path;
        }

        #endregion

        #region Layer

        /// <summary> 対象オブジェクトと同じレイヤーを設定 </summary>
        public static void SetLayer(GameObject sourceObject, GameObject target, bool setChildLayer = false)
        {
            SetLayer(sourceObject.layer, target, setChildLayer);
        }

        /// <summary> 指定レイヤーに設定 </summary>
        public static void SetLayer(int source, GameObject target, bool setChildLayer = false)
        {
            target.layer = source;

            if (setChildLayer)
            {
                for (var i = 0; i < target.transform.childCount; ++i)
                {
                    var trans = target.transform.GetChild(i);

                    trans.gameObject.layer = source;

                    SetLayer(source, trans.gameObject, true);
                }
            }
        }

        #endregion

        #region Component

        /// <summary> コンポーネント取得 </summary>
        public static T GetComponent<T>(Component instance) where T : Component
        {
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <summary> コンポーネント取得 </summary>
        public static T GetComponent<T>(GameObject instance) where T : Component
        {
            return instance != null ? instance.GetComponent<T>() : null;
        }

        /// <summary> コンポーネント取得 </summary>
        public static T[] GetComponents<T>(GameObject instance) where T : Component
        {
            return instance != null ? instance.GetComponents<T>() : new T[0];
        }

        /// <summary> コンポーネント追加 </summary>
        public static T AddComponent<T>(GameObject instance) where T : Component
        {
            return instance != null ? instance.AddComponent<T>() : null;
        }

        /// <summary> コンポーネント取得 (付いてなかったら追加) </summary>
        public static T GetOrAddComponent<T>(GameObject instance) where T : Component
        {
            var result = GetComponent<T>(instance);

            if (result == null)
            {
                result = AddComponent<T>(instance);
            }

            return result;
        }

        /// <summary> インターフェース取得 </summary>
        public static T[] GetInterfaces<T>(GameObject instance) where T : class
        {
            if (instance == null) { return new T[0]; }

            return instance.GetComponents(typeof(MonoBehaviour)).OfType<T>().ToArray();
        }

        /// <summary> インターフェース取得 </summary>
        public static T GetInterface<T>(GameObject instance) where T : class
        {
            if (instance == null) { return null; }

            return instance.GetComponents(typeof(MonoBehaviour)).OfType<T>().FirstOrDefault();
        }

        #endregion

        #region Find Objects

        /// <summary> 型で親オブジェクト検索 </summary>
        public static T FindInParents<T>(GameObject instance) where T : Component
        {
            if (instance == null) { return null; }

            var component = GetComponent<T>(instance);

            if (component == null)
            {
                var t = instance.transform.parent;

                while (t != null && component == null)
                {
                    component = GetComponent<T>(t.gameObject);
                    t = t.parent;
                }
            }
            return component;
        }

        /// <summary>
        /// カメラ取得.
        /// ※ layerMaskは <code>1 &lt;&lt; (int)layer</code>された状態の物を受け取る.
        /// </summary>
        public static Camera[] FindCameraForLayer(int layerMask)
        {
            return FindObjectsOfType<Camera>().Where(x => (x.cullingMask & layerMask) != 0).ToArray();
        }

        /// <summary> 階層オブジェクトを取得 </summary>
        public static List<GameObject> GetChildObjects(GameObject root)
        {
            var list = new List<GameObject>();

            for (var i = 0; i < root.transform.childCount; ++i)
            {
                var trans = root.transform.GetChild(i);

                // 追加.
                list.Add(trans.gameObject);

                // 再帰処理で子階層を追加.
                list.AddRange(GetChildObjects(trans.gameObject));
            }

            return list;
        }

        /// <summary> 子階層オブジェクトを取得 </summary>
        public static List<T> GetChildObject<T>(GameObject root) where T : Component
        {
            var list = new List<T>();

            for (var i = 0; i < root.transform.childCount; ++i)
            {
                var trans = root.transform.GetChild(i);

                T component = GetComponent<T>(trans.gameObject);

                if (component != null)
                {
                    // 追加.
                    list.Add(component);
                }
            }

            return list;
        }

        /// <summary> 全階層オブジェクトを取得 </summary>
        public static List<T> GetChildObjects<T>(GameObject root) where T : Component
        {
            var list = new List<T>();

            for (var i = 0; i < root.transform.childCount; ++i)
            {
                var trans = root.transform.GetChild(i);

                var component = GetComponent<T>(trans.gameObject);

                if (component != null)
                {
                    // 追加.
                    list.Add(component);
                }

                // 再帰処理で子階層を追加.
                list.AddRange(GetChildObjects<T>(trans.gameObject));
            }

            return list;
        }

        /// <summary> Hierarchyから特定の種類のオブジェクト取得 </summary>
        public static T FindObjectOfType<T>() where T : Component
        {
            return FindObjectsOfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Hierarchyから特定の種類のオブジェクト一覧取得.
        /// ※ DontDestroyOnLoad内の非アクティブなオブジェクトは取得不可.
        /// </summary>
        public static T[] FindObjectsOfType<T>() where T : Component
        {
            var targets = new List<T>();

            // DontDestroyOnLoadを設定したオブジェクトはシーン一覧から回収できない.
            // 非アクティブなオブジェクトは回収できないが標準のFindObjectsOfTypeで抽出.
            var dontDestroyObjects = UnityEngine.Object.FindObjectsOfType<T>()
                .Where(x => x.gameObject.scene.name == "DontDestroyOnLoad");

            targets.AddRange(dontDestroyObjects);

            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded) { continue; }

                if (!scene.IsValid()) { continue; }

                var rootObjects = scene.GetRootGameObjects();

                foreach (var rootObject in rootObjects)
                {
                    var objects = FindObjectsOfType<T>(rootObject);

                    targets.AddRange(objects);
                }
            }

            return targets.ToArray();
        }

        /// <summary> Hierarchyから特定の種類のオブジェクト取得 </summary>
        public static T FindObjectOfType<T>(GameObject rootObject) where T : Component
        {
            return FindObjectsOfType<T>(rootObject).FirstOrDefault();
        }

        /// <summary> Hierarchyから特定の種類のオブジェクト一覧取得 </summary>
        public static T[] FindObjectsOfType<T>(GameObject rootObject) where T : Component
        {
            if (rootObject == null) { return new T[0]; }

            var list = new List<T>();

            var objects = rootObject.DescendantsAndSelf();

            foreach (var item in objects)
            {
                var components = GetComponents<T>(item);

                if (components.Any())
                {
                    list.AddRange(components);
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト取得.
        /// </summary>
        public static T FindObjectOfInterface<T>() where T : class
        {
            return FindObjectsOfInterface<T>().FirstOrDefault();
        }

        /// <summary>
        /// Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト取得.
        /// </summary>
        public static T FindObjectOfInterface<T>(GameObject rootObject) where T : class
        {
            return FindObjectsOfInterface<T>(rootObject).FirstOrDefault();
        }

        /// <summary>
        /// Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト一覧取得.
        /// </summary>
        public static T[] FindObjectsOfInterface<T>() where T : class
        {
            var components = FindObjectsOfType<Component>();

            return components.Select(x => x as T).Where(x => x != null).ToArray();
        }

        /// <summary>
        /// Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト一覧取得.
        /// </summary>
        public static T[] FindObjectsOfInterface<T>(GameObject rootObject) where T : class
        {
            var components = FindObjectsOfType<Component>(rootObject);

            return components.Select(x => x as T).Where(x => x != null).ToArray();
        }

        #endregion
    }
}
