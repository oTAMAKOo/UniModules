

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Extensions
{
    public static class UnityUtility
    {
        public static bool isEditor
        {
            get { return Application.installMode == ApplicationInstallMode.Editor; }
        }

        #region Object Instantiate

        private const string PrefabTag = " (Clone)";

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
        public static T CreateGameObject<T>(GameObject parent, string name, bool worldPositionStays = false) where T : Component
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

        /// <summary> パスからPrefab生成 </summary>
        public static GameObject Instantiate(GameObject parent, string path, bool instantiateInWorldSpace = false)
        {
            return Instantiate(parent, (GameObject)Resources.Load(path), instantiateInWorldSpace);
        }

        /// <summary> インスタンス生成 </summary>
        public static GameObject Instantiate(GameObject parent, Component component, bool instantiateInWorldSpace = false)
        {
            if (component == null){ return null; }

            return Instantiate(parent, component.gameObject, instantiateInWorldSpace);
        }

        /// <summary> インスタンス生成 </summary>
        public static GameObject Instantiate(GameObject parent, GameObject original, bool instantiateInWorldSpace = false)
        {
            if (original != null)
            {
                GameObject gameObject = null;

                if (parent == null)
                {
                    gameObject = UnityEngine.Object.Instantiate(original);
                }
                else
                {
                    gameObject = UnityEngine.Object.Instantiate(original, parent.transform, instantiateInWorldSpace);
                }

                if (gameObject != null)
                {
                    gameObject.transform.name = original.name + PrefabTag;
                }

                return gameObject;
            }

            return null;
        }

        /// <summary> 複数のインスタンスを高速生成 </summary>
        public static IEnumerable<GameObject> Instantiate(GameObject parent, Component component, int count, bool instantiateInWorldSpace = false)
        {
            if (component == null) { return null; }

            return Instantiate(parent, component.gameObject, count, instantiateInWorldSpace);
        }

        /// <summary> 複数のインスタンスを高速生成 </summary>
        public static IEnumerable<GameObject> Instantiate(GameObject parent, GameObject original, int count, bool instantiateInWorldSpace = false)
        {
            if (original == null){ return new GameObject[0]; }

            var list = new List<GameObject>();

            var instanceName = string.Empty;

            var sourceObject = original;

            for (var i = 0; i < count; i++)
            {
                var item = Instantiate(parent, sourceObject, instantiateInWorldSpace);

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

            return list;
        }

        /// <summary> Resourcesパスから生成 + Component取得 </summary>
        public static T Instantiate<T>(GameObject parent, string path, bool instantiateInWorldSpace = false) where T : Component
        {
            var gameObject = Instantiate(parent, path, instantiateInWorldSpace);

            return GetComponent<T>(gameObject);
        }

        /// <summary> インスタンス生成 + Component取得 </summary>
        public static T Instantiate<T>(GameObject parent, Component component, bool instantiateInWorldSpace = false) where T : Component
        {
            if (component == null) { return null; }

            return Instantiate<T>(parent, component.gameObject, instantiateInWorldSpace);
        }

        /// <summary> インスタンス生成 + Component取得 </summary>
        public static T Instantiate<T>(GameObject parent, GameObject original, bool instantiateInWorldSpace = false) where T : Component
        {
            var gameObject = Instantiate(parent, original, instantiateInWorldSpace);

            return GetComponent<T>(gameObject);
        }

        /// <summary> 複数のインスタンスを高速生成 + Component取得 </summary>
        public static IEnumerable<T> Instantiate<T>(GameObject parent, Component component, int count, bool instantiateInWorldSpace = false) where T : Component
        {
            if (component == null) { return null; }
            
            return Instantiate<T>(parent, component.gameObject, count, instantiateInWorldSpace);
        }

        /// <summary> 複数のインスタンスを高速生成 + Component取得 </summary>
        public static IEnumerable<T> Instantiate<T>(GameObject parent, GameObject original, int count, bool instantiateInWorldSpace = false) where T : Component
        {
            var gameObjects = Instantiate(parent, original, count, instantiateInWorldSpace);
            
            return gameObjects.Select(x => GetComponent<T>(x));
        }

        #endregion

        #region Object Delete

        /// <summary> オブジェクトの削除 </summary>
        public static void SafeDelete(Component component, bool immediate = false)
        {
            if (component == null) { return; }

            if (IsNull(component.gameObject)) { return; }

            SafeDelete(component.gameObject, immediate);
        }
         
        /// <summary> オブジェクトの削除 </summary>
        public static void SafeDelete(UnityEngine.Object instance, bool immediate = false)
        {
            if (instance == null) { return; }
            
            var gameObject = instance as GameObject;

            SetActive(gameObject, false);

            if (!Application.isPlaying || immediate)
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
            else
            {
                UnityEngine.Object.Destroy(instance);
            }
        }

        public static void DeleteComponent<T>(T component, bool immediate = false) where T : Component
        {
            if (component != null)
            {
                if (!Application.isPlaying || immediate)
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
                else
                {
                    UnityEngine.Object.Destroy(component);
                }
            }
        }

        public static void DeleteComponent<T>(GameObject instance, bool immediate = false) where T : Component
        {
            var component = GetComponent<T>(instance);

            DeleteComponent(component, immediate);
        }

        public static void DeleteGameObject<T>(T component, bool immediate = false) where T : Component
        {
            if (component != null)
            {
                if (!Application.isPlaying || immediate)
                {
                    UnityEngine.Object.DestroyImmediate(component.gameObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(component.gameObject);
                }
            }
        }

        public static void DeleteGameObject<T>(GameObject instance, bool immediate = false) where T : Component
        {
            var component = GetComponent<T>(instance);

            DeleteGameObject<T>(component, immediate);
        }

        /// <summary> 複数オブジェクトの削除 </summary>
        public static void DeleteGameObject(IEnumerable<GameObject> targets)
        {
            foreach (var target in targets)
            {
                SafeDelete(target);
            }
        }

        /// <summary> Nullチェック </summary>
        public static bool IsNull(object obj)
        {
            var unityObj = obj as UnityEngine.Object;

            if (!ReferenceEquals(unityObj, null))
            {
                return unityObj == null;
            }

            return obj == null;
        }

        #endregion

        #region Active Control

        /// <summary> 状態取得 </summary>
        public static bool IsActive(GameObject instance)
        {
            return !IsNull(instance) && instance.activeSelf;
        }

        /// <summary> 状態取得 </summary>
        public static bool IsActive<T>(T instance) where T : Component
        {
            return !IsNull(instance) && instance.gameObject.activeSelf;
        }

        /// <summary> 階層内状態取得 </summary>
        public static bool IsActiveInHierarchy(GameObject instance)
        {
            return !IsNull(instance) && instance.activeInHierarchy;
        }

        /// <summary> 階層内状態取得 </summary>
        public static bool IsActiveInHierarchy<T>(T instance) where T : Component
        {
            return !IsNull(instance) && instance.gameObject.activeInHierarchy;
        }

        /// <summary> 状態設定 </summary>
        public static void SetActive(GameObject instance, bool state)
        {
            if (IsNull(instance)) { return; }

            if (state == instance.activeSelf) { return; }

            instance.SetActive(state);
        }

        /// <summary> 状態設定 </summary>
        public static void SetActive<T>(T instance, bool state) where T : Component
        {
            if (IsNull(instance)) { return; }

            if (state == instance.gameObject.activeSelf) { return; }

            instance.gameObject.SetActive(state);
        }

        #endregion

        #region Object Control

        /// <summary> 親オブジェクト設定 </summary>
        public static void SetParent(GameObject instance, GameObject parent, bool worldPositionStays = false)
        {
            if (!IsNull(instance))
            {
                instance.transform.SetParent(parent != null ? parent.transform : null, worldPositionStays);
            }
        }

        /// <summary> 親オブジェクト設定 </summary>
        public static void SetParent(Component instance, Component parent, bool worldPositionStays = false)
        {
            if (!IsNull(instance))
            {
                instance.transform.SetParent(parent != null ? parent.transform : null, worldPositionStays);
            }
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
        public static IEnumerable<T> GetComponents<T>(GameObject instance) where T : Component
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
        public static IEnumerable<T> GetInterfaces<T>(GameObject instance) where T : class
        {
            if (instance == null) { return new T[0]; }

            return instance.GetComponents(typeof(MonoBehaviour)).OfType<T>();
        }

        /// <summary> インターフェース取得 </summary>
        public static T GetInterface<T>(GameObject instance) where T : class
        {
            if (instance == null) { return null; }

            return instance.GetComponents(typeof(MonoBehaviour)).OfType<T>().FirstOrDefault();
        }

        #endregion

        #region Find Objects

        /// <summary> 自身と子階層のGameObjectを取得 </summary>
        public static IEnumerable<GameObject> GetChildrenAndSelf(GameObject root)
        {
            if (root == null){ yield break; }

            yield return root;

            foreach (Transform item in root.transform)
            {
                foreach (var child in GetChildrenAndSelf(item.gameObject))
                {
                    yield return child.gameObject;
                }
            }
        }

        /// <summary>
        /// <para>カメラ取得.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        /// </summary>
        /// <param name="layerMask">1 &lt;&lt; (int)layer された状態の物を受け取る</param>
        public static Camera[] FindCameraForLayer(int layerMask)
        {
            return FindObjectsOfType<Camera>().Where(x => (x.cullingMask & layerMask) != 0).ToArray();
        }

        /// <summary> Hierarchyから特定の種類のオブジェクト取得 </summary>
        public static T FindObjectOfType<T>() where T : Component
        {
            return FindObjectsOfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// <para>Hierarchyから特定の種類のオブジェクト一覧取得.</para>
        /// <para>※ DontDestroyOnLoad内の非アクティブなオブジェクトは取得不可.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        /// </summary>
        public static IEnumerable<T> FindObjectsOfType<T>() where T : Component
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

            return targets;
        }

        /// <summary>
        /// <para>Hierarchyから特定の種類のオブジェクト取得.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        ///</summary>
        public static T FindObjectOfType<T>(GameObject rootObject) where T : Component
        {
            if (rootObject == null) { return null; }
            
            var objects = GetChildrenAndSelf(rootObject);

            foreach (var item in objects)
            {
                var components = GetComponents<T>(item).ToArray();

                if (components.Any())
                {
                    return components.First();
                }
            }

            return null;
        }

        /// <summary>
        /// <para>Hierarchyから特定の種類のオブジェクト一覧取得.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        ///</summary>
        public static IEnumerable<T> FindObjectsOfType<T>(GameObject rootObject) where T : Component
        {
            if (rootObject == null) { return new T[0]; }

            var list = new List<T>();

            var objects = GetChildrenAndSelf(rootObject);

            foreach (var item in objects)
            {
                var components = GetComponents<T>(item).ToArray();

                if (components.Any())
                {
                    list.AddRange(components);
                }
            }

            return list;
        }
        
        /// <summary>
        /// <para>Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト取得.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        ///</summary>
        public static T FindObjectOfInterface<T>() where T : class
        {
            return FindObjectsOfInterface<T>().FirstOrDefault();
        }

        /// <summary>
        /// <para>Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト取得.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        ///</summary>
        public static T FindObjectOfInterface<T>(GameObject rootObject) where T : class
        {
            return FindObjectsOfInterface<T>(rootObject).FirstOrDefault();
        }

        /// <summary>
        /// <para>Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト一覧取得.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        ///</summary>
        public static IEnumerable<T> FindObjectsOfInterface<T>() where T : class
        {
            var components = FindObjectsOfType<Component>();

            return components.Select(x => x as T).Where(x => x != null);
        }

        /// <summary>
        /// <para>Hierarchyから指定されたインターフェイスを実装したコンポーネントを持つオブジェクト一覧取得.</para>
        /// <para>※ 検索負荷が高いので呼び出し頻度に注意してください.</para>
        ///</summary>
        public static IEnumerable<T> FindObjectsOfInterface<T>(GameObject rootObject) where T : class
        {
            var components = FindObjectsOfType<Component>(rootObject);

            return components.Select(x => x as T).Where(x => x != null);
        }

        #endregion

        #region HierarchyPath

        /// <summary> Hierarchy上のオブジェクトのパス取得 </summary>
        public static string GetHierarchyPath(GameObject target)
        {
            if (target == null){ return string.Empty; }
            
            var trans = target.transform;

            var builder = new StringBuilder();

            while (trans != null)
            {
                if (0 < builder.Length)
                {
                    builder.Insert(0, "/");
                }

                builder.Insert(0, trans.name);

                trans = trans.parent;
            }

            return builder.ToString();
        }

        /// <summary> Hierarchy上の子オブジェクトのパス取得 </summary>
        public static string GetChildHierarchyPath(GameObject root, GameObject target)
        {
            var trans = target.transform.parent;

            var builder = new StringBuilder();

            while (trans != null && root != trans.gameObject)
            {
                if (0 < builder.Length)
                {
                    builder.Insert(0, "/");
                }

                builder.Insert(0, trans.name);

                trans = trans.parent;
            }

            return builder.ToString();
        }

        #endregion
    }
}
