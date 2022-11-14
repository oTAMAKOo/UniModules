
#if ENABLE_XLUA

using UnityEngine;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using XLua;
using Extensions;

namespace Modules.Lua
{
	[LuaCallCSharp]
    public sealed class LuaController : LifetimeDisposable
	{
        //----- params -----

        //----- field -----

		private LuaEnv luaEnv = null;

		private LuaReference luaReference = null;

		private LuaLoader luaLoader = null;

		private string functionName = null;

		//----- property -----

		public LuaEnv LuaEnv { get { return luaEnv; } }

		public LuaReference LuaReference { get { return luaReference; } }

		public bool IsExecute { get; private set; }

		//----- method -----

		public LuaController()
		{
			Observable.EveryUpdate()
				.Subscribe(_ => LuaUpdate())
				.AddTo(Disposable);
		}

		public void Setup(LuaLoader loader, LuaReference reference)
		{
			luaLoader = loader;
			luaReference = reference;

			luaEnv = new LuaEnv();

			luaEnv.AddLoader(GetLuaBytes);

			luaLoader.Initialize(this);

			var autoLoadTargets = reference.Infos.Where(x => x.autoload).ToArray();

			foreach (var target in autoLoadTargets)
			{
				if (string.IsNullOrEmpty(target.path)){ continue; }

				Require(target.path);
			}

			IsExecute = false;
		}

		public void Require(string luaPath)
		{
			luaEnv.Require(luaPath);
		}

		public void Request(string luaPath)
		{
			luaEnv.Request(luaPath);
		}

		private byte[] GetLuaBytes(ref string luaPath)
		{
			string lua = null;

			if (luaPath == "_main_")
			{
				const string format = @"
					__main = {}

					local _mainFunc = async(function()

						await(#LUA_FUNCTION#)

						_luaController:OnFinish()

					end)

					__main.callback = _mainFunc
				";

				var callFunction = FixLuaFunctionCallName(functionName);

				lua = format.Replace("#LUA_FUNCTION#", callFunction);
			}

			if (luaPath == "_log_")
			{
				lua = @"
					log = function(...)
						_luaController:LuaLog(debug.traceback(), ...)
					end

					logf = function(format, ...)
						_luaController:LuaLogFormat(debug.traceback(), format, ...)
					end
				";
			}

			return lua != null ? Encoding.UTF8.GetBytes(lua) : null;
		}

		public async UniTask Prepare()
		{
			// ※ 一瞬遅れて読み込まれるケースの為少し遅延して判定.
			while (luaLoader.IsLoading)
			{
				await UniTask.WaitWhile(() => luaLoader.IsLoading);

				await UniTask.DelayFrame(3);
			}
		}

		public async UniTask Execute(string functionName)
		{
			this.functionName = functionName;

			try
			{
				IsExecute = true;

				Require("framework.AsyncTask");

				Require("_main_");
				Require("_log_");

				luaEnv.Global.Set("_luaController", this);

				luaEnv.DoString("__main.callback()");

				while (IsExecute)
				{
					await UniTask.NextFrame();
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"xLua exception : {ex.Message}\n {ex.StackTrace}");
			}
			finally
			{
				Exit();
			}
		}

		public void Exit()
		{
			if (luaEnv != null)
			{
				luaEnv.Dispose();
				luaEnv = null;
			}

			IsExecute = false;

			Debug.LogWarning("Exit");
		}

		public void OnFinish()
		{
			IsExecute = false;

			Debug.LogWarning("OnFinish");
		}

		private void LuaUpdate()
		{
			if (!IsExecute){ return; }

			if (luaEnv == null){ return; }

			luaEnv.Tick();
		}

		/// <summary> LuaCallback </summary>
		public void LuaLog(string stackTrace, params object[] args)
		{
			using (new DisableStackTraceScope())
			{
				var text = string.Join(", ", args);

				Debug.Log($"LUA: {text}\n\n{stackTrace}");
			}
		}

		/// <summary> LuaCallback </summary>
		public void LuaLogFormat(string stackTrace, string format, params object[] args)
		{
			using (new DisableStackTraceScope())
			{
				var text = string.Format(format, args);

				Debug.LogFormat($"LUA: {text}\n\n{stackTrace}");
			}
		}

		public string FixLuaFunctionCallName(string luaFunction)
		{
			var callName = luaFunction.Replace(" ", string.Empty);
			
			return callName.EndsWith("()") ? callName : callName + "()";
		}

		public T GetValue<T>(string key)
		{
			return luaEnv.Global.Get<T>(key);
		}

		public void SetValue<T>(string key, T value)
		{
			luaEnv.Global.Set(key, value);
		}
	}
}

#endif