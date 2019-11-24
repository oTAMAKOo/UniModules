
#if ENABLE_MOONSHARP

using UnityEngine;
using Extensions;
using MoonSharp.Interpreter;

namespace Modules.Lua
{
    public abstract class LuaClass : LifetimeDisposable
    {
        //----- params -----

        //----- field -----

        protected LuaController luaController = null;
        protected Table apiTable = null;

        //----- property -----

        /// <summary>
        /// 登録関数の戻り値にすると'coroutine.yield'を発行した状態になる.
        /// </summary>
        public static DynValue YieldWait
        {
            get { return DynValue.NewYieldReq(new DynValue[0]); }
        }

        public abstract string LuaName { get; }

        //----- method -----

        public bool SetLuaController(LuaController luaController)
        {
            this.luaController = luaController;

            if (!string.IsNullOrEmpty(LuaName))
            {
                apiTable = luaController.AddGlobalTable(LuaName);
            }
            else
            {
                Debug.LogErrorFormat("Empty name is not allowed.\n{0}", this.GetType().FullName);

                return false;
            }

            return true;
        }

        public abstract void RegisterAPITable();
    }
}

#endif