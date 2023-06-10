
#if ENABLE_XLUA

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using XLua;
using Extensions;

namespace Modules.Lua
{
    [LuaCallCSharp]
    public abstract class LuaLoader : LifetimeDisposable
    {
        //----- params -----

        private const string LuaFileExtension = ".lua";
        
        //----- field -----

        protected LuaController luaController = null;
        
        private HashSet<string> loading = null;

        private Dictionary<string, LuaAsset> loadedAssets = null;

        //----- property -----

        public bool IsLoading { get { return loading.Any(); } }

        //----- method -----

        public void Initialize(LuaController luaController)
        {
            this.luaController = luaController;

            loading = new HashSet<string>();
            loadedAssets = new Dictionary<string, LuaAsset>();

            luaController.SetValue("loader", this);

            luaController.LuaEnv.AddLoader(GetLuaBytes);

            luaController.LuaEnv.Require("module.LazyRequire");

            OnInitialize();
        }

        protected virtual byte[] GetLuaBytes(ref string luaPath)
        {
            LuaAsset luaAsset = null;

            // LuaReferenceから取得.
            
            if (luaAsset == null)
            {
                luaAsset = luaController.LuaReference.GetLuaAsset(luaPath);
            }

            // 読み込み済み.

            if (luaAsset == null)
            {
                luaAsset = loadedAssets.GetValueOrDefault(luaPath);
            }

            return luaAsset != null ? luaAsset.GetData() : null;
        }

        /// <summary> [Lua] 読み込みリクエスト </summary>
        public void LoadRequest(string luaPath)
        {
            // 既に読み込み中の場合は待機.
            if (loading.Contains(luaPath)){ return; }

            // 読み込み.

            loading.Add(luaPath);

            var filePath = ConvertFilePath(luaPath);

            Action<LuaAsset> onComplete = luaAsset =>
            {
                if (luaAsset != null)
                {
                    loadedAssets[luaPath] = luaAsset;

                    luaController.LuaEnv.Require(luaPath);
                }

                loading.Remove(luaPath);
            };

            LoadAsync(filePath).ToObservable()
                .Subscribe(x => onComplete.Invoke(x))
                .AddTo(Disposable);
        }

        private string ConvertFilePath(string luaPath)
        {
            var filePath = luaPath.Replace(".", "/");

            if (!filePath.EndsWith(LuaFileExtension))
            {
                filePath = Path.ChangeExtension(filePath, LuaFileExtension);
            }

            return filePath;
        }

        protected virtual void OnInitialize(){  }

        protected abstract UniTask<LuaAsset> LoadAsync(string filePath);
    }
}

#endif