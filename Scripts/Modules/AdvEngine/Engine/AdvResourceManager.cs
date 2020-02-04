
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
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

        private Dictionary<string, object> resourceLibrary = null;
        
        private Dictionary<Type, Dictionary<string, string>> fileNameLibrary = null;

        private Dictionary<Type, GetPathFunction> getPathFunctions = null;

        //----- property -----

        //----- method -----

        public AdvResourceManager()
        {
            requests = new Dictionary<string, IObservable<Unit>>();
            resourceLibrary = new Dictionary<string, object>();
            fileNameLibrary = new Dictionary<Type, Dictionary<string, string>>();
            getPathFunctions = new Dictionary<Type, GetPathFunction>();
        }

        /// <summary> リクエスト追加. </summary>
        public void Request(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) { return; }

            if (requests.ContainsKey(resourcePath)) { return; }

            requests.Add(resourcePath, Observable.Defer(() => UpdateAsset(resourcePath)));
        }

        /// <summary> リクエスト追加. </summary>
        public void Request<T>(string resourcePath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(resourcePath)) { return; }

            if (resourceLibrary.ContainsKey(resourcePath)) { return; }

            if (requests.ContainsKey(resourcePath)) { return; }

            requests.Add(resourcePath, Observable.Defer(() => LoadAsset<T>(resourcePath)));
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

            resourceLibrary[path] = resource;

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

            if (resourceLibrary.ContainsKey(path))
            {
                resourceLibrary.Remove(path);
            }
        }

        /// <summary> 全リソース解放. </summary>
        public void ReleaseAll()
        {
            requests.Clear();
            resourceLibrary.Clear();
            fileNameLibrary.Clear();
        }

        /// <summary> リソース取得. </summary>
        public T Get<T>(string path) where T : UnityEngine.Object 
        {            
            var resource = resourceLibrary.GetValueOrDefault(path);

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

        /// <summary> ファイル識別子とファイル名を登録. </summary>
        public void RegisterFileName<T>(string fileIdentifier, string fileName)
        {
            var library = fileNameLibrary.GetValueOrDefault(typeof(T));

            if (library == null)
            {
                library = new Dictionary<string, string>();

                fileNameLibrary.Add(typeof(T), library);
            }

            library[fileIdentifier] = fileName;
        }

        /// <summary> ファイル識別子からファイル名取得. </summary>
        public string FindFileName<T>(string fileIdentifier)
        {
            var library = fileNameLibrary.GetValueOrDefault(typeof(T));

            if (library == null) { return null; }

            if (!library.ContainsKey(fileIdentifier))
            {
                throw new FileNotFoundException(string.Format("Not found fileIdentifier : {0}", fileIdentifier));
            }

            return library[fileIdentifier];
        }

        protected abstract IObservable<Unit> UpdateAsset(string resourcePath);

        protected abstract IObservable<Unit> LoadAsset<T>(string resourcePath) where T : UnityEngine.Object;
    }
}

#endif
