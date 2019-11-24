
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

using Coroutine = MoonSharp.Interpreter.Coroutine;

namespace Modules.Lua
{
    public sealed class LuaController
    {
        //----- params -----

        //----- field -----
        
        private Dictionary<Type, LuaClass> classTable = null;

        private Coroutine luaCoroutine = null;

        //----- property -----

        public Script LuaScript { get; private set; }

        public IScriptLoader ScriptLoader { get; private set; }

        //----- method -----

        public LuaController()
        {
            LuaVector.RegisterAll();

            classTable = new Dictionary<Type, LuaClass>();

            LuaScript = new Script(CoreModules.Preset_Default)
            {
                Options =
                {
                    DebugPrint = log => Debug.Log(log),
                }
            };
        }
        
        public LuaController(HashSet<Type> luaTypes) : this()
        {
            if (luaTypes != null)
            {
                foreach (var luaType in luaTypes)
                {
                    if (!luaType.IsSubclassOf(typeof(LuaClass)))
                    {
                        Debug.LogErrorFormat("This class is not subclass of LuaClass.\nclass : {0}", luaType.FullName);
                        continue;
                    }

                    var luaClass = Activator.CreateInstance(luaType) as LuaClass;

                    if (luaClass != null)
                    {
                        var setup = luaClass.SetLuaController(this);

                        if (setup)
                        {
                            luaClass.RegisterAPITable();

                            classTable.Add(luaType, luaClass);
                        }
                    }
                }
            }
        }

        public void SetScriptLoader(IScriptLoader scriptLoader)
        {
            ScriptLoader = scriptLoader;

            LuaScript.Options.ScriptLoader = ScriptLoader;           
        }

        /// <summary>
        /// Lua制御クラスインスタンスを取得.
        /// </summary>
        public T GetLuaClass<T>() where T : LuaClass
        {
            return classTable.GetValueOrDefault(typeof(T)) as T;
        }

        /// <summary>
        /// スクリプトを待機状態から復旧.
        /// </summary>
        public void Resume()
        {
            if (luaCoroutine == null) { return; }

            if (luaCoroutine.State == CoroutineState.Suspended)
            {
                luaCoroutine.Resume();
            }
        }

        /// <summary>
        /// Luaスクリプト実行.
        /// </summary>
        public IObservable<Unit> ExecuteScript(string script)
        {
            return Observable.FromMicroCoroutine(() => ExecuteScriptInternal(script));
        }

        private IEnumerator ExecuteScriptInternal(string script)
        {
            var luaFunc = DynValue.Nil;

            try
            {
                luaFunc = LuaScript.DoString(script);
            }
            catch (InterpreterException ex)
            {
                Debug.LogErrorFormat("Lua Execute error:\n{0}", ex.DecoratedMessage);
            }

            if (luaFunc.IsNil()) { yield break; }

            luaCoroutine = null;

            var dynValue = LuaScript.CreateCoroutine(luaFunc);

            if (dynValue.IsNil()) { yield break; }

            luaCoroutine = dynValue.Coroutine;

            luaCoroutine.AutoYieldCounter = 1000;

            luaCoroutine.Resume();

            while (luaCoroutine.State != CoroutineState.Dead)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Luaスクリプト読み込み.
        /// </summary>
        public void LoadScript(string script)
        {
            try
            {
                var luaFunc = LuaScript.LoadString(script);

                if (luaFunc.IsNotNil() && luaFunc.Type == DataType.Function)
                {
                    luaFunc.Function.Call();
                }
            }
            catch (InterpreterException ex)
            {
                Debug.LogErrorFormat("Lua ExecuteString error: {0}", ex.DecoratedMessage);
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("System ExecuteString error: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Luaスクリプト関数実行.
        /// </summary>
        public void CallFunction(DynValue function, params object[] args)
        {
            if (function.IsNotNil() && function.Type == DataType.Function)
            {
                try
                {
                    LuaScript.Call(function, args);
                }
                catch (ScriptRuntimeException ex)
                {
                    Debug.LogErrorFormat("Lua Call error:\n{0}", ex.DecoratedMessage);
                }
            }
            else
            {
                Debug.LogErrorFormat("Invalid lua function passed to LuaController.Call");
            }
        }

        /// <summary>
        /// Luaスクリプト関数実行.
        /// </summary>
        public void CallFunction(string functionName, params object[] args)
        {
            var function = GetGlobal(functionName);

            CallFunction(function, args);
        }

        public bool SetGlobal(string key, object value)
        {
            var didSet = false;

            try
            {
                LuaScript.Globals[key] = value;

                didSet = true;
            }
            catch (InterpreterException ex)
            {
                Debug.LogErrorFormat("Lua SetGlobal error: {0}", ex.DecoratedMessage);
            }

            return didSet;
        }

        public DynValue GetGlobal(string key)
        {
            var result = DynValue.Nil;

            try
            {
                result = LuaScript.Globals.Get(key);
            }
            catch
            {
                Debug.LogErrorFormat("Failed to get Lua global {0}", key);
            }

            return result;
        }

        public DynValue GetGlobal(params object[] keys)
        {
            var result = DynValue.Nil;

            try
            {
                result = LuaScript.Globals.Get(keys);
            }
            catch
            {
                var error = string.Join(", ", Array.ConvertAll(keys, input => input.ToString()));

                Debug.LogErrorFormat("Failed to get Lua global at '{0}'", error);
            }

            return result;
        }

        public Table AddGlobalTable(string tableName)
        {
            Table table = null;

            if (SetGlobal(tableName, DynValue.NewTable(LuaScript)))
            {
                table = GetGlobal(tableName).Table;
            }
            else
            {
                Debug.LogErrorFormat("Failed to add global Lua table {0}", tableName);
            }

            return table;
        }

        public Table GetGlobalTable(string key)
        {
            Table result = null;

            var tableDyn = GetGlobal(key);

            if (tableDyn != null)
            {
                if (tableDyn.Type == DataType.Table)
                {
                    result = tableDyn.Table;
                }
                else
                {
                    Debug.LogErrorFormat("Lua global {0} is not type table, has type {1}", key, tableDyn.Type.ToString());
                }
            }
            return result;
        }

        public Table GetGlobalTable(params object[] keys)
        {
            Table result = null;

            var tableDyn = GetGlobal(keys);

            if (tableDyn != null)
            {
                if (tableDyn.Type == DataType.Table)
                {
                    result = tableDyn.Table;
                }
                else
                {
                    Debug.LogErrorFormat("Lua global {0} is not type table, has type {1}", keys, tableDyn.Type.ToString());
                }
            }
            return result;
        }
    }
}

#endif