
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;
using Modules.Scene.Diagnostics;

namespace Modules.Scene
{
    public abstract partial class SceneManagerBase<TInstance, TScenes> : Singleton<TInstance>　
        where TInstance : SceneManagerBase<TInstance, TScenes>
        where TScenes : struct, Enum
    {
        //----- params -----

        public readonly string ConsoleEventName = "Scene";
        public readonly Color ConsoleEventColor = new Color(0.4f, 1f, 0.4f);

        //----- field -----

        protected CancellationTokenSource transitionCancelSource = null;
        protected CancellationTokenSource preLoadCancelSource = null;

        protected Dictionary<TScenes, SceneInstance<TScenes>> loadedScenes = null;
        protected FixedQueue<SceneInstance<TScenes>> cacheScenes = null;

        protected Dictionary<TScenes, IObservable<SceneInstance<TScenes>>> loadingScenes = null;
        protected Dictionary<TScenes, IObservable<Unit>> unloadingScenes = null;

        protected SceneInstance<TScenes> currentScene = null;
        protected ISceneArgument<TScenes> currentSceneArgument = null;

        protected List<SceneInstance<TScenes>> appendSceneInstances = null;

        protected List<ISceneArgument<TScenes>> history = null;

        private Subject<SceneInstance<TScenes>> onPrepare = null;
        private Subject<SceneInstance<TScenes>> onPrepareComplete = null;

        private Subject<SceneInstance<TScenes>> onEnter = null;
        private Subject<SceneInstance<TScenes>> onEnterComplete = null;

        private Subject<SceneInstance<TScenes>> onLeave = null;
        private Subject<SceneInstance<TScenes>> onLeaveComplete = null;

        private Subject<SceneInstance<TScenes>> onLoadScene = null;
        private Subject<SceneInstance<TScenes>> onLoadSceneComplete = null;
        private Subject<Unit> onLoadError = null;

        private Subject<SceneInstance<TScenes>> onUnloadScene = null;
        private Subject<SceneInstance<TScenes>> onUnloadSceneComplete = null;
        private Subject<Unit> onUnloadError = null;

        private Subject<Unit> onCancel = null;

        //----- property -----

        /// <summary> 現在のシーン情報 </summary>
        public SceneInstance<TScenes> Current { get { return currentScene; } }

        /// <summary> 読み込み済みシーン情報 </summary>
        public IReadOnlyList<SceneInstance<TScenes>> LoadedScenesInstances
        {
            get { return loadedScenes.Values.ToArray(); }
        }

        /// <summary> 追加読み込み済みシーン情報 </summary>
        public IReadOnlyList<SceneInstance<TScenes>> AppendSceneInstances
        {
            get { return appendSceneInstances; }
        }

        /// <summary> 遷移中か </summary>
        public bool IsTransition { get; private set; }

        /// <summary> 遷移先のシーン </summary>
        public TScenes? TransitionTarget { get; private set; }

        /// <summary> キャッシュするシーン数 </summary>
        protected virtual int CacheSize { get { return 3; } }

        /// <summary> シーンを読み込む為の定義情報 </summary>
        protected abstract Dictionary<TScenes, string> ScenePaths { get; }

        //----- method -----

        protected SceneManagerBase() { }

        protected override void OnCreate()
        {
            transitionCancelSource = new CancellationTokenSource();

            loadedScenes = new Dictionary<TScenes, SceneInstance<TScenes>>();
            cacheScenes = new FixedQueue<SceneInstance<TScenes>>(CacheSize);

            loadingScenes = new Dictionary<TScenes, IObservable<SceneInstance<TScenes>>>();
            unloadingScenes = new Dictionary<TScenes, IObservable<Unit>>();

            appendSceneInstances = new List<SceneInstance<TScenes>>();

            history = new List<ISceneArgument<TScenes>>();
            waitHandlerIds = new HashSet<int>();

            capturedComponents = new Dictionary<Type, Behaviour>();
            suspendOriginStatus = new Dictionary<Behaviour, bool>();

            // キャッシュ許容数を超えたらアンロード.
            cacheScenes.OnExtrudedAsObservable()
                .Subscribe(x => UnloadCacheScene(x))
                .AddTo(Disposable);

            AppendModuleInitialize();
        }

        public async UniTask RegisterBootScene()
        {
            if (currentScene != null) { return; }

            var scene = SceneManager.GetSceneAt(0);

            while (!scene.isLoaded)
            {
                await UniTask.DelayFrame(1);
            }

            var definition = ScenePaths.FirstOrDefault(x => x.Value == scene.path);
            var identifier = definition.Equals(default(KeyValuePair<TScenes, string>)) ? null : (TScenes?)definition.Key;

            var sceneInstance = UnityUtility.FindObjectsOfInterface<ISceneBase<TScenes>>().FirstOrDefault();

            CollectUniqueComponents(scene.GetRootGameObjects());

            if (sceneInstance != null)
            {
                currentScene = new SceneInstance<TScenes>(identifier, sceneInstance, SceneManager.GetSceneAt(0));
            }

            if (currentScene == null || currentScene.Instance == null)
            {
                Debug.LogError("Current scene not found.");

                return;
            }

            // 初期化.

            var argumentType = currentScene.Instance.GetArgumentType();

            var sceneArgument = Activator.CreateInstance(argumentType) as ISceneArgument<TScenes>;

            await currentScene.Instance.SetArgument(sceneArgument);

            await OnRegisterCurrentScene(currentScene);
            
            history.Add(sceneArgument);

            // シーン登録.

            loadedScenes.Add(sceneArgument.Identifier.Value, currentScene);

            // 起動シーンフラグ設定.

            var sceneBase = currentScene.Instance as SceneBase<TScenes>;

            if (sceneBase != null)
            {
                sceneBase.SetLaunchScene();
            }

            // ISceneEvent発行.

            var tasks = new List<UniTask>();

            if (sceneBase != null)
            {
                var targets = UnityUtility.GetInterfaces<ISceneEvent>(sceneBase.gameObject);

                foreach (var target in targets)
                {
                    var task = UniTask.Defer(() => target.OnLoadScene());

                    tasks.Add(task);
                }
            }

            await UniTask.WhenAll(tasks);

            // Initialize.
        
            await currentScene.Instance.Initialize();

            // Prepare.

            if (onPrepare != null)
            {
                onPrepare.OnNext(currentScene);
            }

            await currentScene.Instance.Prepare();

            if (onPrepareComplete != null)
            {
                onPrepareComplete.OnNext(currentScene);
            }

            // Enter.

            if (onEnter != null)
            {
                onEnter.OnNext(currentScene);
            }

            currentScene.Instance.Enter();

            if (onEnterComplete != null)
            {
                onEnterComplete.OnNext(currentScene);
            }

            // PreLoad.

            if (sceneArgument.PreLoadScenes != null && sceneArgument.PreLoadScenes.Any())
            {
                PreLoadScene(sceneArgument.PreLoadScenes).Forget();
            }
        }

        /// <summary> 初期シーン登録時のイベント </summary>
        protected virtual UniTask OnRegisterCurrentScene(SceneInstance<TScenes> currentInfo)
        {
            return UniTask.CompletedTask;
        }

        /// <summary> シーン遷移. </summary>
        public void Transition<TArgument>(TArgument sceneArgument, bool registerHistory = false, LoadSceneMode mode = LoadSceneMode.Additive) 
            where TArgument : ISceneArgument<TScenes>
        {
            // 遷移中は遷移不可.
            if (IsTransition) { return; }

            IsTransition = true;

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            ObservableEx.FromUniTask(cancelToken => TransitionCore(sceneArgument, mode, false, registerHistory, cancelToken))
                .Subscribe(_ => IsTransition = false)
                .AddTo(transitionCancelSource.Token);
        }

        /// <summary> シーン再読み込み. </summary>
        public void Reload()
        {
            // 遷移中は遷移不可.
            if (IsTransition) { return; }

            IsTransition = true;

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            ObservableEx.FromUniTask(cancelToken => TransitionCore(currentSceneArgument, LoadSceneMode.Additive, false, false, cancelToken))
                .Subscribe(_ => IsTransition = false)
                .AddTo(transitionCancelSource.Token);
        }

        /// <summary>
        /// シーン遷移を中止.
        /// </summary>
        public void TransitionCancel()
        {
            if (transitionCancelSource != null && !transitionCancelSource.IsCancellationRequested)
            {
                transitionCancelSource.Cancel();
            }

            transitionCancelSource = new CancellationTokenSource();

            IsTransition = false;

            if (onCancel != null)
            {
                onCancel.OnNext(Unit.Default);
            }
        }

        /// <summary>
        /// １つ前のシーンに遷移.
        /// </summary>
        /// <returns></returns>
        public void TransitionBack()
        {
            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.

            ISceneArgument<TScenes> argument = null;

            // 遷移中は遷移不可.
            if (TransitionTarget != null) { return; }

            // 履歴の一番最後尾の要素は現在のシーンなので、history[要素数 - 2]が前回のシーンになる.
            if (1 < history.Count)
            {
                argument = history[history.Count - 2];
                history.Remove(argument);
            }

            if (argument != null)
            {
                IsTransition = true;

                ObservableEx.FromUniTask(cancelToken => TransitionCore(argument, LoadSceneMode.Additive, true, false, cancelToken))
                    .Subscribe(_ => IsTransition = false)
                    .AddTo(transitionCancelSource.Token);
            }
        }

        /// <summary>
        /// シーン遷移履歴をクリア.
        /// </summary>
        public void ClearTransitionHistory()
        {
            ISceneArgument<TScenes> currentEntity = null;

            // 現在のシーンの情報は残す.
            if (history.Any())
            {
                currentEntity = history.Last();
            }

            history.Clear();

            if (currentEntity != null)
            {
                history.Add(currentEntity);
            }
        }

        /// <summary> シーン遷移の引数履歴取得 </summary>
        public ISceneArgument<TScenes>[] GetArgumentHistory()
        {
            return history.ToArray();
        }

        /// <summary> キャッシュが存在するか. </summary>
        public bool HasCache(TScenes scene)
        {
            return cacheScenes.Any(x => x.Identifier.Equals(scene));
        }

        private async UniTask TransitionCore<TArgument>(TArgument argument, LoadSceneMode mode, bool isSceneBack, bool registerHistory, CancellationToken cancelToken) 
            where TArgument : ISceneArgument<TScenes>
        {
            if (!argument.Identifier.HasValue) { return; }

            if (mode == LoadSceneMode.Additive)
            {
                // ロード済みシーンからの遷移制御.

                var handleTransition = await HandleTransitionFromLoadedScenes();

                if (!handleTransition) { return; }
            }

            // 事前ロードキャンセル.
            if (preLoadCancelSource != null)
            {
                preLoadCancelSource.Cancel();
            }

            // 遷移開始.

            TransitionTarget = argument.Identifier;

            var prevSceneArgument = currentSceneArgument;

            currentSceneArgument = argument;

            var diagnostics = new TimeDiagnostics();

            var prev = currentScene;

            // 現在のシーンを履歴に残さない場合は、既に登録済みの現在のシーン(history[要素数 - 1])を外す.
            if (!registerHistory && history.Any())
            {
                history.RemoveAt(history.Count - 1);
            }

            //====== Begin Transition ======

            diagnostics.Begin(TimeDiagnostics.Measure.Total);

            try
            {
                await TransitionStart(currentSceneArgument, isSceneBack);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (cancelToken.IsCancellationRequested){ return; }


            //====== Scene Leave ======

            // Leave呼び出し対象.

            var leaveScenes = new HashSet<SceneInstance<TScenes>> { prev };

            // Singleの場合はAppend済みのシーンも対象.
            if (mode == LoadSceneMode.Single)
            {
                foreach (var appendSceneInstance in appendSceneInstances)
                {
                    leaveScenes.Add(appendSceneInstance);
                }
            }

            if (leaveScenes.Any())
            {
                diagnostics.Begin(TimeDiagnostics.Measure.Leave);

                foreach (var leaveScene in leaveScenes)
                {
                    // Leave通知.
                    if (onLeave != null)
                    {
                        onLeave.OnNext(leaveScene);
                    }

                    // 現在のシーンの終了処理を実行.
                    try
                    {
                        await leaveScene.Instance.Leave();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    if (cancelToken.IsCancellationRequested) { return; }

                    // PlayerPrefsを保存.
                    PlayerPrefs.Save();

                    // Leave終了通知.
                    if (onLeaveComplete != null)
                    {
                        onLeaveComplete.OnNext(leaveScene);
                    }

                    leaveScene.Disable();
                }

                diagnostics.Finish(TimeDiagnostics.Measure.Leave);
            }

            //====== Scene Unload ======

            if (mode == LoadSceneMode.Additive)
            {
                bool IsUnloadTarget(SceneInstance<TScenes> sceneInstance)
                {
                    // SceneBaseクラスが存在しない.
                    if (UnityUtility.IsNull(sceneInstance.Instance)) { return true; }

                    var result = true;

                    // 遷移元のシーンではない.
                    result &= sceneInstance != prev;

                    // 遷移先のシーンではない.
                    result &= !sceneInstance.Identifier.Equals(TransitionTarget);

                    // キャッシュ対象でない.
                    result &= cacheScenes.All(x => x != sceneInstance);

                    // 次のシーンのPreLoad対象ではない.
                    result &= currentSceneArgument.PreLoadScenes.All(y => !y.Equals(sceneInstance.Identifier));

                    return result;
                };

                // 不要なシーンをアンロード.
                var unloadScenes = loadedScenes.Values.Where(x => IsUnloadTarget(x)).ToArray();

                foreach (var unloadScene in unloadScenes)
                {
                    try
                    {
                        _ = await UnloadScene(unloadScene);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    if (unloadScene.Identifier.HasValue)
                    {
                        loadedScenes.Remove(unloadScene.Identifier.Value);
                    }
                }
            } 
            else
            {
                // LoadSceneMode.Singleの場合読み込み済みシーンが全て消える.

                loadedScenes.Clear();
                appendSceneInstances.Clear();
                cacheScenes.Clear();
            }

            //====== Load Next Scene ======

            diagnostics.Begin(TimeDiagnostics.Measure.Load);
            
            // 次のシーンを読み込み.
            var identifier = argument.Identifier.Value;

            var sceneInstance = loadedScenes.GetValueOrDefault(identifier);

            if (sceneInstance == null)
            {
                try
                {
                    sceneInstance = await LoadScene(identifier, mode);
                }
                catch (Exception e)
                {
                    OnLoadError(e, identifier);
                }

                if (sceneInstance == null) { return; }

                if (argument.Cache)
                {
                    cacheScenes.Enqueue(sceneInstance);
                }
            }

            var scene = sceneInstance.GetScene();

            if (!scene.HasValue)
            {
                Debug.LogErrorFormat("[ {0} ] : Failed to get Scene information.", identifier);

                return;
            }

            if (sceneInstance.Instance == null)
            {
                Debug.LogErrorFormat("[ {0} ] : SceneBase class does not exist.", scene.Value.path);

                return;
            }

            if (cancelToken.IsCancellationRequested){ return; }

            SetSceneActive(scene);

            // 前のシーンからの引数を設定.
            await sceneInstance.Instance.SetArgument(argument);

            // 現在のシーンとして登録.
            currentScene = sceneInstance;

            // シーン戻り登録.
            if (currentScene.Instance != null)
            {
                currentScene.Instance.SetSceneBack(isSceneBack);
            }

            // 次のシーンを履歴に登録.
            // シーン引数を保存する為遷移時に引数と一緒に履歴登録する為、履歴の最後尾は現在のシーンになる.
            if (currentScene.Instance != null)
            {
                history.Add(currentSceneArgument);
            }

            // シーン読み込み後にAwake、Startが終わるのを待つ為1フレーム後に処理を再開.
            await UniTask.NextFrame(cancelToken);

            if (cancelToken.IsCancellationRequested){ return; }

            diagnostics.Finish(TimeDiagnostics.Measure.Load);

            //====== Scene Prepare ======

            diagnostics.Begin(TimeDiagnostics.Measure.Prepare);

            // Prepare通知.
            if (onPrepare != null)
            {
                onPrepare.OnNext(currentScene);
            }

            // 次のシーンの準備処理実行.
            if (currentScene.Instance != null)
            {
                try
                {
                    await currentScene.Instance.Prepare();
                }
                catch (OperationCanceledException) 
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (cancelToken.IsCancellationRequested) { return; }
            }

            // Prepare終了通知.
            if (onPrepareComplete != null)
            {
                onPrepareComplete.OnNext(currentScene);
            }

            diagnostics.Finish(TimeDiagnostics.Measure.Prepare);

            //====== Unload PrevScene ======

            // キャッシュ対象でない場合はアンロード.
            if (prev != currentScene)
            {
                if (prevSceneArgument == null || !prevSceneArgument.Cache)
                {
                    try
                    {
                        await UnloadScene(prev).ToUniTask(cancellationToken: cancelToken);
                    }
                    catch (OperationCanceledException) 
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            //====== Scene Wait ======

            // メモリ解放.

            try
            {
                await CleanUp();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (cancelToken.IsCancellationRequested) { return; }

            // 外部処理待機.

            try
            {
                await TransitionWait();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (cancelToken.IsCancellationRequested) { return; }

            // シーンを有効化.
            sceneInstance.Enable();

            // シーン遷移完了.
            TransitionTarget = null;

            // レイアウト更新待ち.
            await UniTask.NextFrame(cancellationToken:CancellationToken.None);

            //====== TransitionFinish ======

            try
            {
                await currentScene.Instance.OnTransition();

                await TransitionFinish(currentSceneArgument, isSceneBack);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (cancelToken.IsCancellationRequested) { return; }

            //====== Scene Enter ======

            // Enter通知.
            if (onEnter != null)
            {
                onEnter.OnNext(currentScene);
            }

            // 次のシーンの開始処理実行.
            if (currentScene.Instance != null)
            {
                currentScene.Instance.Enter();
            }

            // Enter終了通知.
            if (onEnterComplete != null)
            {
                onEnterComplete.OnNext(currentScene);
            }

            //====== Report ======

            diagnostics.Finish(TimeDiagnostics.Measure.Total);

            var prevScene = prev.Identifier;
            var nextScene = currentScene.Identifier;

            var total = diagnostics.GetTime(TimeDiagnostics.Measure.Total);
            var detail = diagnostics.BuildDetailText();

            var message = $"{prevScene} → {nextScene} ({total:F2}ms)\n\n{detail}";

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

            //====== PreLoad ======

            PreLoadScene(argument.PreLoadScenes).Forget();
        }

        private async UniTask<bool> HandleTransitionFromLoadedScenes()
        {
            var enableScenes = loadedScenes.Values.Where(x => x.IsEnable);

            foreach (var sceneInstance in enableScenes)
            {
                var transitionHandler = sceneInstance.Instance as ITransitionHandler;

                if (transitionHandler == null){ continue; }

                // シーンから遷移が許可されたか.
                var result = await transitionHandler.HandleTransition();
            
                if (!result)
                {
                    var message = $"Transition cancel request from : {sceneInstance.Identifier}"; 

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

                    return false;
                }
            }

            return true;
        }

        #region Scene Load
        
        private IObservable<SceneInstance<TScenes>> LoadScene(TScenes identifier, LoadSceneMode mode)
        {
            var observable = loadingScenes.GetValueOrDefault(identifier);

            if (observable == null)
            {
                observable = LoadSceneCore(identifier, mode)
                    .ToObservable()
                    .Do(_ => loadingScenes.Remove(identifier))
                    .Share();

                loadingScenes.Add(identifier, observable);
            }

            return observable;
        }

        private async UniTask<SceneInstance<TScenes>> LoadSceneCore(TScenes identifier, LoadSceneMode mode)
        {
            var sceneInstance = loadedScenes.GetValueOrDefault(identifier);

            if (sceneInstance == null)
            {
                var scenePath = ScenePaths.GetValueOrDefault(identifier);

                UnityAction<UnityEngine.SceneManagement.Scene, LoadSceneMode> sceneLoaded = (s, m) =>
                {
                    if (!s.IsValid()){ return; }
                    
                    sceneInstance = new SceneInstance<TScenes>(identifier, FindSceneObject(s), s);

                    switch (m)
                    {
                        case LoadSceneMode.Single:
                            loadedScenes.Clear();
                            cacheScenes.Clear();
                            break;
                    }

                    var rootObjects = s.GetRootGameObjects();

                    // UniqueComponentsを回収.
                    CollectUniqueComponents(rootObjects);

                    // 退避したコンポーネント復元.
                    ResumeCapturedComponents();

                    // 初期状態は非アクティブ.
                    sceneInstance.Disable();

                    if (onLoadScene != null)
                    {
                        onLoadScene.OnNext(sceneInstance);
                    }
                };

                SceneManager.sceneLoaded += sceneLoaded;

                AsyncOperation op = null;

                try
                {
                    op = SceneManager.LoadSceneAsync(scenePath, mode);

                    op.allowSceneActivation = false;

                    while (op.progress < 0.9f)
                    {
                        await UniTask.NextFrame();
                    }
                    
                    // コンポーネントを退避.
                    SuspendCapturedComponents();

                    op.allowSceneActivation = true;

                    while (!op.isDone)
                    {
                        await UniTask.NextFrame();
                    }
                }
                finally
                {
                    SceneManager.sceneLoaded -= sceneLoaded;
                }

                var scene = sceneInstance.GetScene();

                if (scene.HasValue)
                {
                    loadedScenes.Add(identifier, sceneInstance);

                    // 1フレーム待つ.
                    await UniTask.NextFrame();

                    if (onLoadSceneComplete != null)
                    {
                        onLoadSceneComplete.OnNext(sceneInstance);
                    }

                    // ISceneEvent発行.

                    var tasks = new List<UniTask>();

                    var sceneBase = currentScene.Instance as SceneBase<TScenes>;

                    if (sceneBase != null)
                    {
                        var targets = UnityUtility.GetInterfaces<ISceneEvent>(sceneBase.gameObject);

                        foreach (var target in targets)
                        {
                            var task = UniTask.Defer(() => target.OnLoadScene());

                            tasks.Add(task);
                        }
                    }

                    await UniTask.WhenAll(tasks);
                }

                // シーンの初期化処理.
                if (sceneInstance.Instance != null)
                {
                    try
                    {
                        await sceneInstance.Instance.Initialize();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            else
            {
                // 初期状態は非アクティブ.
                sceneInstance.Disable();
            }

            return sceneInstance;
        }

        private void OnLoadError(Exception exception, TScenes? identifier)
        {
            Debug.LogErrorFormat("Load scene error : {0}", identifier);

            Debug.LogException(exception);

            if (onLoadError != null)
            {
                onLoadError.OnNext(Unit.Default);
            }
        }

        public IObservable<SceneInstance<TScenes>> OnLoadSceneAsObservable()
        {
            return onLoadScene ?? (onLoadScene = new Subject<SceneInstance<TScenes>>());
        }

        public IObservable<SceneInstance<TScenes>> OnLoadSceneCompleteAsObservable()
        {
            return onLoadSceneComplete ?? (onLoadSceneComplete = new Subject<SceneInstance<TScenes>>());
        }

        public IObservable<Unit> OnLoadErrorAsObservable()
        {
            return onLoadError ?? (onLoadError = new Subject<Unit>());
        }

        #endregion

        #region Scene Unload

        /// <summary> シーンを指定してアンロード. </summary>
        public void UnloadScene(TScenes identifier)
        {
            if (currentScene.Identifier.Equals(identifier))
            {
                throw new ArgumentException("The current scene can not be unloaded");
            }

            var sceneInstance = loadedScenes.GetValueOrDefault(identifier);

            // AddTo(this)されると途中でdisposableされてしまうのでIObservableで公開しない.
            UnloadScene(sceneInstance).Subscribe().AddTo(Disposable);
        }

        private IObservable<Unit> UnloadScene(SceneInstance<TScenes> sceneInstance)
        {
            if (sceneInstance == null) { return Observable.ReturnUnit(); }

            if (!sceneInstance.Identifier.HasValue) { return Observable.ReturnUnit(); }

            var scene = sceneInstance.GetScene();

            if (!scene.HasValue){ return Observable.ReturnUnit(); }

            // メインシーンはアンロードできない.
            
            var mainScene = SceneManager.GetActiveScene();

            if (mainScene == scene)
            {
                Debug.LogWarning($"Main scene {sceneInstance.Identifier} is cannot be unloaded.");

                return Observable.ReturnUnit();
            }

            // アンロード.
            
            var identifier = sceneInstance.Identifier.Value;

            var observable = unloadingScenes.GetValueOrDefault(identifier);

            if (observable == null)
            {
                observable = UnloadSceneCore(sceneInstance)
                    .ToObservable()
                    .AsUnitObservable()
                    .Do(_ => unloadingScenes.Remove(identifier))
                    .Share();

                unloadingScenes.Add(identifier, observable);
            }

            return observable;
        }

        private async UniTask UnloadSceneCore(SceneInstance<TScenes> sceneInstance)
        {
            var scene = sceneInstance.GetScene();

            if (!scene.HasValue) { return; }

            if (!scene.Value.isLoaded) { return; }

            if (SceneManager.sceneCount <= 1){ return; }

            UnityAction<UnityEngine.SceneManagement.Scene> sceneUnloaded = s =>
            {
                if (s.IsValid())
                {
                    if (sceneInstance.Identifier.HasValue)
                    {
                        var identifier = sceneInstance.Identifier.Value;

                        if (loadedScenes.ContainsKey(identifier))
                        {
                            loadedScenes.Remove(identifier);
                        }
                    }

                    if (cacheScenes.Contains(sceneInstance))
                    {
                        cacheScenes.Remove(sceneInstance);
                    }

                    if (onUnloadScene != null)
                    {
                        onUnloadScene.OnNext(sceneInstance);
                    }
                }
            };

            // ISceneEvent発行.

            var tasks = new List<UniTask>();

            var sceneBase = sceneInstance.Instance as SceneBase<TScenes>;

            if (sceneBase != null)
            {
                var targets = UnityUtility.GetInterfaces<ISceneEvent>(sceneBase.gameObject);

                foreach (var target in targets)
                {
                    var task = UniTask.Defer(() => target.OnUnloadScene());

                    tasks.Add(task);
                }
            }

            await UniTask.WhenAll(tasks);

            // Scene Unload.

            AsyncOperation op = null;

            try
            {
                SceneManager.sceneUnloaded += sceneUnloaded;

                op = SceneManager.UnloadSceneAsync(scene.Value);

                while (op != null && !op.isDone)
                {
                    await UniTask.NextFrame();
                }
            }
            catch (OperationCanceledException) 
            {
                return;
            }
            catch (Exception e)
            {
                SceneManager.sceneUnloaded -= sceneUnloaded;

                Debug.LogException(e);

                if (onUnloadError != null)
                {
                    onUnloadError.OnNext(Unit.Default);
                }

                return;
            }

            SceneManager.sceneUnloaded -= sceneUnloaded;

            await CleanUp();

            if (onUnloadSceneComplete != null)
            {
                onUnloadSceneComplete.OnNext(sceneInstance);
            }
        }

        public IObservable<SceneInstance<TScenes>> OnUnloadSceneAsObservable()
        {
            return onUnloadScene ?? (onUnloadScene = new Subject<SceneInstance<TScenes>>());
        }

        public IObservable<SceneInstance<TScenes>> OnUnloadSceneCompleteAsObservable()
        {
            return onUnloadSceneComplete ?? (onUnloadSceneComplete = new Subject<SceneInstance<TScenes>>());
        }

        public IObservable<Unit> OnUnloadErrorAsObservable()
        {
            return onUnloadError ?? (onUnloadError = new Subject<Unit>());
        }

        #endregion

        #region Scene Preload

        /// <summary> 事前読み込み. </summary>
        private async UniTask PreLoadScene(TScenes[] targetScenes)
        {
            if (preLoadCancelSource != null && !preLoadCancelSource.IsCancellationRequested)
            {
                preLoadCancelSource.Cancel();
                preLoadCancelSource.Dispose();
            }

            preLoadCancelSource = new CancellationTokenSource();

            if (targetScenes.IsEmpty()) { return; }

            var builder = new StringBuilder();
            
            var sw = System.Diagnostics.Stopwatch.StartNew();

            foreach (var scene in targetScenes)
            {
                // ロード済みのシーンがある場合はプリロードしない.
                if (loadedScenes.Values.Any(x => x.Identifier.Equals(scene))) { continue; }

                // キャッシュ済みのシーンがある場合はプリロードしない.
                if (cacheScenes.Any(x => x.Identifier.Equals(scene))) { continue; }

                try
                {
                    await PreLoadCore(scene, builder);

                    await UniTask.NextFrame(preLoadCancelSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Canceled.
                }

                if (preLoadCancelSource.IsCancellationRequested){ return; }
            }

            sw.Stop();

            var time = sw.Elapsed.TotalMilliseconds;
            var detail = builder.ToString();

            var message = $"PreLoad Complete ({time:F2}ms)\n\n{detail}";

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
        }

        private async UniTask PreLoadCore(TScenes targetScene, StringBuilder builder)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await LoadScene(targetScene, LoadSceneMode.Additive).ToUniTask();
            }
            catch (Exception e)
            {
                OnLoadError(e, targetScene);
            }

            sw.Stop();

            var time = sw.Elapsed.TotalMilliseconds;

            builder.AppendLine($"{targetScene} ({time:F2}ms)");
        }

        #endregion

        #region Scene Cache

        /// <summary> キャッシュ済みの全シーンをアンロード. </summary>
        public IObservable<Unit> UnloadAllCacheScene()
        {
            var sceneInstances = cacheScenes.ToArray();

            return sceneInstances.Select(x => UnloadScene(x)).WhenAll().AsUnitObservable();
        }

        /// <summary> キャッシュ済みのシーンをアンロード. </summary>
        private void UnloadCacheScene(SceneInstance<TScenes> sceneInstance)
        {
            // ※ 現在のシーンは破棄できんないので再度キャッシュキューに登録しなおす.

            if (sceneInstance == currentScene)
            {
                cacheScenes.Enqueue(sceneInstance);
            }
            else
            {
                if (sceneInstance.Identifier.HasValue)
                {
                    cacheScenes.Remove(sceneInstance);
                }

                UnloadScene(sceneInstance).Subscribe().AddTo(Disposable);
            }
        }

        #endregion

        /// <summary> シーンが展開済みか </summary>
        public bool IsSceneLoaded(TScenes identifier)
        {
            return loadedScenes.ContainsKey(identifier);
        }

        /// <summary> シーンを取得 </summary>
        public SceneInstance<TScenes> GetSceneInstance(TScenes identifier)
        {
            return loadedScenes.GetValueOrDefault(identifier);
        }

        private ISceneBase<TScenes> FindSceneObject(UnityEngine.SceneManagement.Scene scene)
        {
            ISceneBase<TScenes> sceneBase = null;

            if (!scene.isLoaded || !scene.IsValid()) { return null; }

            var rootObjects = scene.GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                sceneBase = UnityUtility.FindObjectOfInterface<ISceneBase<TScenes>>(rootObject);

                if (sceneBase != null)
                {
                    break;
                }
            }

            return sceneBase;
        }

        private UnityEngine.SceneManagement.Scene[] GetAllScenes()
        {
            var scenes = new List<UnityEngine.SceneManagement.Scene>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                scenes.Add(SceneManager.GetSceneAt(i));
            }

            return scenes.ToArray();
        }

        private void SetSceneActive(UnityEngine.SceneManagement.Scene? scene)
        {
            if (!scene.HasValue) { return; }

            if (!scene.Value.IsValid()) { return; }

            SceneManager.SetActiveScene(scene.Value);
        }

        private async UniTask CleanUp()
        {
            var unloadOperation = Resources.UnloadUnusedAssets();

            while (!unloadOperation.isDone)
            {
                await UniTask.NextFrame();
            }

            GC.Collect();
        }

        //====== Prepare Scene ======

        public IObservable<SceneInstance<TScenes>> OnPrepareAsObservable()
        {
            return onPrepare ?? (onPrepare = new Subject<SceneInstance<TScenes>>());
        }

        public IObservable<SceneInstance<TScenes>> OnPrepareCompleteAsObservable()
        {
            return onPrepareComplete ?? (onPrepareComplete = new Subject<SceneInstance<TScenes>>());
        }

        //====== Enter Scene ======

        public IObservable<SceneInstance<TScenes>> OnEnterAsObservable()
        {
            return onEnter ?? (onEnter = new Subject<SceneInstance<TScenes>>());
        }

        public IObservable<SceneInstance<TScenes>> OnEnterCompleteAsObservable()
        {
            return onEnterComplete ?? (onEnterComplete = new Subject<SceneInstance<TScenes>>());
        }

        //====== Leave Scene ======

        public IObservable<SceneInstance<TScenes>> OnLeaveAsObservable()
        {
            return onLeave ?? (onLeave = new Subject<SceneInstance<TScenes>>());
        }

        public IObservable<SceneInstance<TScenes>> OnLeaveCompleteAsObservable()
        {
            return onLeaveComplete ?? (onLeaveComplete = new Subject<SceneInstance<TScenes>>());
        }

        //====== Cancel ======

        public IObservable<Unit> OnCancelAsObservable()
        {
            return onCancel ?? (onCancel = new Subject<Unit>());
        }

        protected abstract UniTask TransitionStart<TArgument>(TArgument sceneArgument, bool isSceneBack) where TArgument : ISceneArgument<TScenes>;

        protected abstract UniTask TransitionFinish<TArgument>(TArgument sceneArgument, bool isSceneBack) where TArgument : ISceneArgument<TScenes>;
    }
}
