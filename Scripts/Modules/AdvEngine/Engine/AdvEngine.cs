
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using Extensions;
using Modules.Lua;

namespace Modules.AdvKit
{
    public sealed class AdvEngine : Singleton<AdvEngine>
    {
        //----- params -----

        //----- field -----

        private string luaScript = null;

        private LuaController luaController = null;

        private LuaScriptLoader scriptLoader = null;

        private HashSet<Type> commandControllerTypes = null;

        private Dictionary<Type, CommandController> commandControllers = null;

        private Dictionary<Type, Command> commandDictionary = null;

        private float timeScale = 1f;

        private Subject<Unit> onLoad = null;
        private Subject<Unit> onExecute = null;
        private Subject<Unit> onFinish = null;
        private Subject<Unit> onQuit = null;

        private Subject<float> onTimeScaleChange = null;

        private bool initialized = false;

        //----- property -----

        public bool IsExecute { get; private set; }

        public AdvObjectManager ObjectManager { get; private set; }

        public AdvResourceManager Resource { get; private set; }

        public AdvSoundManager Sound { get; private set; }

        public float TimeScale
        {
            get { return timeScale; }

            set
            {
                timeScale = value;

                if (onTimeScaleChange != null)
                {
                    onTimeScaleChange.OnNext(timeScale);
                }
            }
        }
        
        //----- method -----

        public void Initialize(GameObject rootObject,
                               AdvResourceManager resourceManager, AdvSoundManager soundManager,
                               HashSet<Type> commandControllerTypes, AdvObjectSetting advObjectSetting)
        {
            if (initialized) { return; }
            
            this.commandControllerTypes = commandControllerTypes;

            Resource = resourceManager;
            Sound = soundManager;

            ObjectManager = new AdvObjectManager();

            ObjectManager.Initialize(rootObject, advObjectSetting);

            IsExecute = false;

            initialized = true;
        }

        public void SetScriptLoader(LuaScriptLoader scriptLoader)
        {
            this.scriptLoader = scriptLoader;
        }

        private void SetupLua()
        {
            luaController = new LuaController(commandControllerTypes);

            luaController.SetScriptLoader(scriptLoader);

            commandControllers = new Dictionary<Type, CommandController>();

            foreach (var luaType in commandControllerTypes)
            {
                var commandController = luaController.GetLuaClass(luaType) as CommandController;

                commandControllers.Add(luaType, commandController);
            }

            commandDictionary = commandControllers.Values
                .SelectMany(x => x.GetAllCommands())
                .ToDictionary(x => x.GetType());
        }

        public IObservable<Unit> Load(string luaScript)
        {
            this.luaScript = luaScript;

            SetupLua();

            Resource.ReleaseAll();
            ObjectManager.DeleteAll();
            ObjectManager.ResetRoot();

            Action onLoadComplete = () =>
            {
                if (onLoad != null)
                {
                    onLoad.OnNext(Unit.Default);
                }
            };

            return luaController.LoadScript(luaScript).Do(_ => onLoadComplete());
        }

        public IObservable<Unit> Execute()
        {
            foreach (var item in commandDictionary.Values)
            {
                item.Initialize();
            }

            IsExecute = true;

            if (onExecute != null)
            {
                onExecute.OnNext(Unit.Default);
            }

            Action finishCallBack = () =>
            {
                IsExecute = false;
                
                if (onFinish != null)
                {
                    onFinish.OnNext(Unit.Default);
                }
            };

            return luaController.ExecuteScript(luaScript)
                .Do(_ => finishCallBack())
                .AsUnitObservable();
        }

        public void Quit()
        {
            IsExecute = false;

            if (luaController != null)
            {
                luaController.QuitScript();

                luaController = null;
            }
            
            if (onQuit != null)
            {
                onQuit.OnNext(Unit.Default);
            }
        }

        public TCommand GetCommandClass<TCommand>() where TCommand : Command
        {
            if (commandDictionary == null) { return null; }

            return commandDictionary.GetValueOrDefault(typeof(TCommand)) as TCommand;
        }

        public void SetGlobal(string key, object value)
        {
            luaController.SetGlobal(key, value);
        }

        public void Resume()
        {
            luaController.Resume();
        }

        public void SetTweenTimeScale(Tweener tweener)
        {
            if (tweener == null) { return; }

            tweener.timeScale = timeScale;

            OnTimeScaleChangeAsObservable()
                .TakeWhile(_ => !tweener.IsComplete())
                .Subscribe(x => tweener.timeScale = x)
                .AddTo(Disposable);
        }
        
        /// <summary> 読み込み時イベント. </summary>
        public IObservable<Unit> OnLoadAsObservable()
        {
            return onLoad ?? (onLoad = new Subject<Unit>());
        }

        /// <summary> 再生開始時イベント. </summary>
        public IObservable<Unit> OnExecuteAsObservable()
        {
            return onExecute ?? (onExecute = new Subject<Unit>());
        }

        /// <summary> 再生終了時イベント. </summary>
        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }

        /// <summary> 強制終了時イベント. </summary>
        public IObservable<Unit> OnQuitAsObservable()
        {
            return onQuit ?? (onQuit = new Subject<Unit>());
        }

        /// <summary> 速度変更イベント. </summary>
        public IObservable<float> OnTimeScaleChangeAsObservable()
        {
            return onTimeScaleChange ?? (onTimeScaleChange = new Subject<float>());
        }
    }
}

#endif
