
#if ENABLE_XLUA

using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;
using Modules.Lua;
using Modules.Lua.Command;
using Modules.Lua.Text;
using Modules.TimeUtil;

namespace Modules.Scenario
{
    public abstract class ScenarioController
    {
        //----- params -----

        //----- field -----

        private CancellationTokenSource cancelSource = null;

        //----- property -----

        public string LuaPath { get; private set; }

        public LuaReference LuaReference { get; private set; }

        public LuaController LuaController { get; private set; }

        public LuaLoader LuaLoader { get; private set; }

        public LuaText LuaText { get; private set; }

        public CommandLoader CommandLoader { get; private set; }

        public TimeScale TimeScale { get; private set; }

        public ManagedObjects ManagedObjects { get; private set; }

        public AssetController AssetController { get; private set; }

        public TaskController TaskController { get; private set; }

        #if ENABLE_CRIWARE_ADX

        public SoundController SoundController { get; private set; }

        #endif

        //----- method -----

        public void Setup(string luaPath, LuaReference luaReference)
        {
            cancelSource = new CancellationTokenSource();

            var aesCryptoKey = GetCryptoKey();

            LuaPath = luaPath;
            LuaReference = luaReference;
            LuaText = new LuaText(aesCryptoKey);

            TimeScale = new TimeScale();
            
            ManagedObjects = new ManagedObjects();

            AssetController = new AssetController();
            TaskController = new TaskController();

            #if ENABLE_CRIWARE_ADX

            SoundController = new SoundController();

            #endif

            LuaController = new LuaController();

            LuaLoader = CreateLuaLoader();
            
            LuaController.Setup(LuaLoader, LuaReference);

            CommandLoader = CreateCommandLoader();

            CommandLoader.Setup(LuaController);

            foreach (var command in CommandLoader.Commands.Values)
            {
                var scenarioCommand = command as ScenarioCommand;

                if (scenarioCommand == null){ continue; }

                scenarioCommand.Setup(this);
            }
        }

        /// <summary> 準備処理実行. </summary>
        public async UniTask Prepare(string luaFunction)
        {
            LuaController.Request(LuaPath);

            await LuaController.Prepare();

            var callFunction = LuaController.FixLuaFunctionCallName(luaFunction);

            LuaController.LuaEnv.DoString(callFunction);
        }

        /// <summary> メイン処理実行. </summary>
        public async UniTask Execute(string luaFunction, CancellationToken cancelToken = default)
        {
            var linkedCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, cancelSource.Token);

            var linkedCancelToken = linkedCancelTokenSource.Token;

            try
            {
                UnityConsole.Info($"Scenario Execute:\nLua = {LuaPath}\nFunction = {luaFunction}");

                await LuaController.Execute(luaFunction, linkedCancelToken);

                UnityConsole.Info($"Scenario Finish:\nLua = {LuaPath}\nFunction = {luaFunction}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary> 必要アセット一覧取得. </summary>
        public string[] GetRequestAssets()
        {
            return AssetController.GetAllRequestAssets();
        }

        public T GetValue<T>(string key)
        {
            return LuaController.LuaEnv.Global.Get<T>(key);
        }

        public void SetValue<T>(string key, T value)
        {
            LuaController.LuaEnv.Global.Set(key, value);
        }

        public void Cancel()
        {
            if (cancelSource != null)
            {
                cancelSource.Cancel();
                cancelSource.Dispose();
            }

            cancelSource = new CancellationTokenSource();
        }

        protected abstract LuaLoader CreateLuaLoader();

        protected abstract CommandLoader CreateCommandLoader();

        protected abstract AesCryptoKey GetCryptoKey();
    }
}

#endif