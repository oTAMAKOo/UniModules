
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.AdvKit
{
    public abstract class AdvResourceManager
    {
        //----- params -----

        public delegate string GetPathFunction(string fileName);

        //----- field -----

        private Dictionary<string, IObservable<Unit>> requests = null;

        private Dictionary<string, object> library = null;

        private Dictionary<Type, GetPathFunction> getPathFunctions = null;

        //----- property -----

        //----- method -----

        public AdvResourceManager()
        {
            requests = new Dictionary<string, IObservable<Unit>>();
            library = new Dictionary<string, object>();
            getPathFunctions = new Dictionary<Type, GetPathFunction>();
        }

        /// <summary> リクエスト追加. </summary>
        public void Request(string path)
        {
            if (string.IsNullOrEmpty(path)) { return; }

            if (requests.ContainsKey(path)) { return; }

            requests.Add(path, Observable.Defer(() => UpdateAsset(path)));
        }

        /// <summary> リクエスト追加. </summary>
        public void Request<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path)) { return; }

            if (library.ContainsKey(path)) { return; }

            if (requests.ContainsKey(path)) { return; }

            requests.Add(path, Observable.Defer(() => LoadAsset<T>(path)));
        }

        /// <summary> リクエスト取得. </summary>
        public KeyValuePair<string, IObservable<Unit>>[] GetRequests()
        {
            return requests.ToArray();
        }

        /// <summary> リソース追加. </summary>
        public void Add(string path, object resource)
        {
            if (string.IsNullOrEmpty(path)) { return; }

            if (resource == null) { return; }

            library[path] = resource;

            // リクエスト中のリソースが登録されたらリクエストから除外.
            if (requests.ContainsKey(path))
            {
                requests.Remove(path);
            }
        }

        /// <summary> リソース解放. </summary>
        public void Release(string path)
        {
            if (string.IsNullOrEmpty(path)) { return; }

            if (requests.ContainsKey(path))
            {
                requests.Remove(path);
            }

            if (library.ContainsKey(path))
            {
                library.Remove(path);
            }
        }

        /// <summary> 全リソース解放. </summary>
        public void ReleaseAll()
        {
            requests.Clear();
            library.Clear();
        }

        /// <summary> リソース取得. </summary>
        public T Get<T>(string path) where T : UnityEngine.Object 
        {            
            var resource = library.GetValueOrDefault(path);

            if (resource == null) { return null; }

            return resource as T;
        }

        /// <summary> 指定の型のリソースパス取得関数を登録. </summary>
        public void RegisterGetResourcePathFunction<T>(GetPathFunction function)
        {
            var type = typeof(T);

            getPathFunctions[type] = function;
        }

        /// <summary> 指定の型のリソースパスを取得. </summary>
        public string GetResourcePath<T>(string fileName)
        {
            var path = string.Empty;

            var type = typeof(T);

            var func = getPathFunctions.GetValueOrDefault(type);

            if (func != null)
            {
                path = func.Invoke(fileName);
            }

            return path;
        }

        protected abstract IObservable<Unit> UpdateAsset(string resourcePath);

        protected abstract IObservable<Unit> LoadAsset<T>(string resourcePath) where T : UnityEngine.Object;
    }
}

#endif
