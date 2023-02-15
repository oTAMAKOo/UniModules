
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
using Constants;
using Modules.Devkit.Console;
using Modules.Scene.Diagnostics;

namespace Modules.Scene
{
    public abstract partial class SceneManager<T> : Singleton<T> where T : SceneManager<T>
    {
        //----- params -----

        public readonly string ConsoleEventName = "Scene";
        public readonly Color ConsoleEventColor = new Color(0.4f, 1f, 0.4f);

        //----- field -----

		protected CancellationTokenSource transitionCancelSource = null;

		protected Dictionary<Scenes, SceneInstance> loadedScenes = null;
		protected FixedQueue<SceneInstance> cacheScenes = null;

		protected Dictionary<Scenes, IObservable<SceneInstance>> loadingScenes = null;
		protected Dictionary<Scenes, IObservable<Unit>> unloadingScenes = null;

		protected SceneInstance currentScene = null;
		protected ISceneArgument currentSceneArgument = null;

		protected List<SceneInstance> appendSceneInstances = null;

		protected List<ISceneArgument> history = null;

		protected LifetimeDisposable preLoadDisposable = null;
		
        private Subject<SceneInstance> onPrepare = null;
        private Subject<SceneInstance> onPrepareComplete = null;

        private Subject<SceneInstance> onEnter = null;
        private Subject<SceneInstance> onEnterComplete = null;

        private Subject<SceneInstance> onLeave = null;
        private Subject<SceneInstance> onLeaveComplete = null;

        private Subject<SceneInstance> onLoadScene = null;
        private Subject<SceneInstance> onLoadSceneComplete = null;
        private Subject<Unit> onLoadError = null;

        private Subject<SceneInstance> onUnloadScene = null;
        private Subject<SceneInstance> onUnloadSceneComplete = null;
        private Subject<Unit> onUnloadError = null;

		private Subject<ISceneArgument> onForceTransition = null;

        //----- property -----

        /// <summary> 現在のシーン情報 </summary>
        public SceneInstance Current { get { return currentScene; } }

		/// <summary> 読み込み済みシーン情報 </summary>
		public IReadOnlyList<SceneInstance> LoadedScenesInstances { get { return loadedScenes.Values.ToArray(); } }

		/// <summary> 追加読み込み済みシーン情報 </summary>
		public IReadOnlyList<SceneInstance> AppendSceneInstances { get { return appendSceneInstances; } }

		/// <summary> 遷移中か </summary>
        public bool IsTransition { get; private set; }

        /// <summary> 遷移先のシーン </summary>
        public Scenes? TransitionTarget { get; private set; }

        /// <summary> キャッシュするシーン数 </summary>
        protected virtual int CacheSize { get { return 3; } }

        /// <summary> シーンを読み込む為の定義情報 </summary>
        protected abstract Dictionary<Scenes, string> ScenePaths { get; }

        //----- method -----

        protected SceneManager() { }

        protected override void OnCreate()
        {
			transitionCancelSource = new CancellationTokenSource();

            loadedScenes = new Dictionary<Scenes, SceneInstance>();
            cacheScenes = new FixedQueue<SceneInstance>(CacheSize);

            loadingScenes = new Dictionary<Scenes, IObservable<SceneInstance>>();
            unloadingScenes = new Dictionary<Scenes, IObservable<Unit>>();

            appendSceneInstances = new List<SceneInstance>();

            history = new List<ISceneArgument>();
            waitHandlerIds = new HashSet<int>();

            // キャッシュ許容数を超えたらアンロード.
            cacheScenes.OnExtrudedAsObservable()
                .Subscribe(x => UnloadCacheScene(x))
                .AddTo(Disposable);
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
            var identifier = definition.Equals(default(KeyValuePair<Scenes, string>)) ? null : (Scenes?)definition.Key;

            var sceneInstance = UnityUtility.FindObjectsOfInterface<ISceneBase>().FirstOrDefault();

            CollectUniqueComponents(scene.GetRootGameObjects());

            if (sceneInstance != null)
            {
                currentScene = new SceneInstance(identifier, sceneInstance, SceneManager.GetSceneAt(0));
            }

            if (currentScene == null || currentScene.Instance == null)
            {
                Debug.LogError("Current scene not found.");

                return;
            }

			var argumentType = currentScene.Instance.GetArgumentType();

			var sceneArgument = Activator.CreateInstance(argumentType) as ISceneArgument;

			currentScene.Instance.SetArgument(sceneArgument);

            await OnRegisterCurrentScene(currentScene);
			
			history.Add(sceneArgument);

			// 起動シーンフラグ設定.

			var sceneBase = currentScene.Instance as SceneBase;

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

			await currentScene.Instance.Prepare(false);

            if (onPrepareComplete != null)
            {
                onPrepareComplete.OnNext(currentScene);
            }

			// Enter.

			if (onEnter != null)
            {
                onEnter.OnNext(currentScene);
            }

            currentScene.Instance.Enter(false);

            if (onEnterComplete != null)
            {
                onEnterComplete.OnNext(currentScene);
            }

			// PreLoad.

			if (sceneArgument.PreLoadScenes != null && sceneArgument.PreLoadScenes.Any())
			{
				RequestPreLoad(sceneArgument.PreLoadScenes);
			}
        }

        /// <summary> 初期シーン登録時のイベント </summary>
        protected virtual UniTask OnRegisterCurrentScene(SceneInstance currentInfo)
		{
			return UniTask.CompletedTask;
		}

        /// <summary> シーン遷移. </summary>
        public void Transition<TArgument>(TArgument sceneArgument, bool registerHistory = false) where TArgument : ISceneArgument
        {
            // 遷移中は遷移不可.
            if (IsTransition) { return; }

			IsTransition = true;

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            ObservableEx.FromUniTask(cancelToken => TransitionCore(sceneArgument, LoadSceneMode.Additive, false, registerHistory, false, cancelToken))
                .Subscribe(_ => IsTransition = false)
                .AddTo(transitionCancelSource.Token);
        }

        /// <summary> 強制シーン遷移. </summary>
        public void ForceTransition<TArgument>(TArgument sceneArgument, bool registerHistory = false) where TArgument : ISceneArgument
        {
            TransitionCancel();

			if (onForceTransition != null)
			{
				onForceTransition.OnNext(sceneArgument);
			}

			IsTransition = true;

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            ObservableEx.FromUniTask(cancelToken => TransitionCore(sceneArgument, LoadSceneMode.Single, false, registerHistory, true, cancelToken))
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
            ObservableEx.FromUniTask(cancelToken => TransitionCore(currentSceneArgument, LoadSceneMode.Additive, false, false, false, cancelToken))
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
        }

        /// <summary>
        /// １つ前のシーンに遷移.
        /// </summary>
        /// <returns></returns>
        public void TransitionBack()
        {
            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.

            ISceneArgument argument = null;

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

                ObservableEx.FromUniTask(cancelToken => TransitionCore(argument, LoadSceneMode.Additive, true, false, false, cancelToken))
                    .Subscribe(_ => IsTransition = false)
                    .AddTo(transitionCancelSource.Token);
            }
        }

        /// <summary>
        /// シーン遷移履歴をクリア.
        /// </summary>
        public void ClearTransitionHistory()
        {
            ISceneArgument currentEntity = null;

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
        public ISceneArgument[] GetArgumentHistory()
        {
            return history.ToArray();
        }

		/// <summary> キャッシュが存在するか. </summary>
		public bool HasCahce(Scenes scene)
		{
			return cacheScenes.Any(x => x.Identifier == scene);
		}

        private async UniTask TransitionCore<TArgument>(TArgument argument, LoadSceneMode mode, bool isSceneBack, bool registerHistory, bool force, CancellationToken cancelToken) 
			where TArgument : ISceneArgument
        {
            if (!argument.Identifier.HasValue) { return; }

			if (!force)
			{
				// ロード済みシーンからの遷移制御.

				var handleTransition = await HandleTransitionFromLoadedScenes();

				if (!handleTransition) { return; }
			}

			// プリロード停止.

			if (preLoadDisposable != null)
            {
                preLoadDisposable.Dispose();
                preLoadDisposable = null;
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
				await TransitionStart(currentSceneArgument, isSceneBack).AttachExternalCancellation(cancelToken);
			}
			catch (OperationCanceledException) 
			{
				return;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

            if (prev != null)
            {
                //====== Scene Leave ======

                diagnostics.Begin(TimeDiagnostics.Measure.Leave);

                // Leave通知.
                if (onLeave != null)
                {
                    onLeave.OnNext(prev);
                }

                // 現在のシーンの終了処理を実行.
				try
				{
					await prev.Instance.Leave().AttachExternalCancellation(cancelToken);
				}
				catch (OperationCanceledException) 
				{
					return;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}

				// PlayerPrefsを保存.
                PlayerPrefs.Save();

                // Leave終了通知.
                if (onLeaveComplete != null)
                {
                    onLeaveComplete.OnNext(prev);
                }

                prev.Disable();

                diagnostics.Finish(TimeDiagnostics.Measure.Leave);
            }

            //====== Scene Unload ======

            if (mode == LoadSceneMode.Additive)
            {
                Func<SceneInstance, bool> isUnloadTarget = sceneInstance =>
                {
                    // SceneBaseクラスが存在しない.
                    if (UnityUtility.IsNull(sceneInstance.Instance)) { return true; }

                    var result = true;

                    // 遷移元のシーンではない.
                    result &= sceneInstance != prev;

                    // 遷移先のシーンではない.
                    result &= sceneInstance.Identifier != TransitionTarget;

                    // キャッシュ対象でない.
                    result &= cacheScenes.All(x => x != sceneInstance);

                    // 次のシーンのPreLoad対象ではない.
                    result &= currentSceneArgument.PreLoadScenes.All(y => y != sceneInstance.Identifier);

                    return result;
                };

                // 不要なシーンをアンロード.
                var unloadScenes = loadedScenes.Values.Where(x => isUnloadTarget(x)).ToArray();

                foreach (var unloadScene in unloadScenes)
                {
					try
					{
						await UnloadScene(unloadScene).ToUniTask(cancellationToken: cancelToken);
					}
					catch (OperationCanceledException) 
					{
						return;
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

            var sceneInfo = loadedScenes.GetValueOrDefault(identifier);

            if (sceneInfo == null)
            {
				try
				{
					sceneInfo = await LoadScene(identifier, mode).ToUniTask(cancellationToken: cancelToken);
				}
				catch (OperationCanceledException) 
				{
					return;
				}
				catch (Exception e)
				{
					OnLoadError(e, identifier);
				}

				if (sceneInfo == null) { return; }

				if (argument.Cache)
                {
                    cacheScenes.Enqueue(sceneInfo);
                }
            }

            var scene = sceneInfo.GetScene();

            if (!scene.HasValue)
            {
                Debug.LogErrorFormat("[ {0} ] : Failed to get Scene information.", identifier);

				return;
            }

            if (sceneInfo.Instance == null)
            {
                Debug.LogErrorFormat("[ {0} ] : SceneBase class does not exist.", scene.Value.path);

				return;
            }

            SetSceneActive(scene);

            // 前のシーンからの引数を設定.
            sceneInfo.Instance.SetArgument(argument);

            // 現在のシーンとして登録.
            currentScene = sceneInfo;

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
					await currentScene.Instance.Prepare(isSceneBack).AttachExternalCancellation(cancelToken);
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

            // Prepare終了通知.
            if (onPrepareComplete != null)
            {
                onPrepareComplete.OnNext(currentScene);
            }

            diagnostics.Finish(TimeDiagnostics.Measure.Prepare);

            //====== Unload PrevScene ======

            // キャッシュ対象でない場合はアンロード.
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

            //====== Scene Wait ======

            // メモリ解放.

			try
			{
				await CleanUp().AttachExternalCancellation(cancelToken);
			}
			catch (OperationCanceledException) 
			{
				return;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			// 外部処理待機.

			try
			{
				await TransitionWait().AttachExternalCancellation(cancelToken);
			}
			catch (OperationCanceledException) 
			{
				return;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			// シーンを有効化.
            sceneInfo.Enable();

            // シーン遷移完了.
            TransitionTarget = null;

            // シーン遷移終了.

			try
			{
				await currentScene.Instance.OnTransition().AttachExternalCancellation(cancelToken);

				await TransitionFinish(currentSceneArgument, isSceneBack).AttachExternalCancellation(cancelToken);
			}
			catch (OperationCanceledException) 
			{
				return;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			//====== Scene Enter ======

            // Enter通知.
            if (onEnter != null)
            {
                onEnter.OnNext(currentScene);
            }

            // 次のシーンの開始処理実行.
            if (currentScene.Instance != null)
            {
                currentScene.Instance.Enter(isSceneBack);
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

            var message = string.Format("{0} → {1} ({2:F2}ms)\n\n{3}", prevScene, nextScene, total, detail);

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

            //====== PreLoad ======

            RequestPreLoad(argument.PreLoadScenes);
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
        
        private IObservable<SceneInstance> LoadScene(Scenes identifier, LoadSceneMode mode)
        {
            var observable = loadingScenes.GetValueOrDefault(identifier);

            if (observable == null)
            {
                observable = Observable.Defer(() => ObservableEx.FromUniTask(cancelToken => LoadSceneCore(identifier, mode, cancelToken))
                    .Do(_ => loadingScenes.Remove(identifier)))
                    .Share();

                loadingScenes.Add(identifier, observable);
            }

            return observable;
        }

        private async UniTask<SceneInstance> LoadSceneCore(Scenes identifier, LoadSceneMode mode, CancellationToken cancelToken)
        {
            var sceneInstance = loadedScenes.GetValueOrDefault(identifier);

            if (sceneInstance == null)
            {
                var scenePath = ScenePaths.GetValueOrDefault(identifier);

				UnityAction<UnityEngine.SceneManagement.Scene, LoadSceneMode> sceneLoaded = (s, m) =>
                {
                    if (s.IsValid())
                    {
                        sceneInstance = new SceneInstance(identifier, FindSceneObject(s), s);

                        switch (m)
                        {
                            case LoadSceneMode.Single:
								loadedScenes.Clear();
								cacheScenes.Clear();
                                break;
                        }

                        // 初期状態は非アクティブ.
                        sceneInstance.Disable();

                        if (onLoadScene != null)
                        {
							onLoadScene.OnNext(sceneInstance);
                        }
                    }
                };

                SetEnabledForCapturedComponents(false);

                SceneManager.sceneLoaded += sceneLoaded;

                AsyncOperation op = null;

                try
                {
                    op = SceneManager.LoadSceneAsync(scenePath, mode);

					while (!op.isDone)
					{
						if (cancelToken.IsCancellationRequested){ break; }

						await UniTask.NextFrame(cancelToken);
					}
                }
				catch (OperationCanceledException) 
				{
					return null;
				}
				finally
				{
					SceneManager.sceneLoaded -= sceneLoaded;
				}

                var scene = sceneInstance.GetScene();

                if (scene.HasValue)
                {
                    var rootObjects = scene.Value.GetRootGameObjects();

                    // 回収するオブジェクトが非アクティブ化されているのを一時的に戻す.
                    sceneInstance.Enable();

                    // UniqueComponentsを回収.
                    CollectUniqueComponents(rootObjects);

                    // 回収後オブジェクトの状態を非アクティブ化.
                    sceneInstance.Disable();

                    loadedScenes.Add(identifier, sceneInstance);

                    // 1フレーム待つ.
					await UniTask.NextFrame(cancelToken);

					if (cancelToken.IsCancellationRequested){ return null; }

                    if (onLoadSceneComplete != null)
                    {
                        onLoadSceneComplete.OnNext(sceneInstance);
                    }

					// ISceneEvent発行.

					var tasks = new List<UniTask>();

					var sceneBase = currentScene.Instance as SceneBase;

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

                SetEnabledForCapturedComponents(true);

                // シーンの初期化処理.
                if (sceneInstance.Instance != null)
                {
					try
					{
						await sceneInstance.Instance.Initialize().AttachExternalCancellation(cancelToken);
					}
					catch (OperationCanceledException) 
					{
						return null;
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

        private void OnLoadError(Exception exception, Scenes? identifier)
        {
            Debug.LogErrorFormat("Load scene error : {0}", identifier);

            Debug.LogException(exception);

            if (onLoadError != null)
            {
                onLoadError.OnNext(Unit.Default);
            }
        }

        public IObservable<SceneInstance> OnLoadSceneAsObservable()
        {
            return onLoadScene ?? (onLoadScene = new Subject<SceneInstance>());
        }

        public IObservable<SceneInstance> OnLoadSceneCompleteAsObservable()
        {
            return onLoadSceneComplete ?? (onLoadSceneComplete = new Subject<SceneInstance>());
        }

        public IObservable<Unit> OnLoadErrorAsObservable()
        {
            return onLoadError ?? (onLoadError = new Subject<Unit>());
        }

        #endregion

        #region Scene Unload

        /// <summary> シーンを指定してアンロード. </summary>
        public void UnloadScene(Scenes identifier)
        {
            if ( currentScene.Identifier == identifier)
            {
                throw new ArgumentException("The current scene can not be unloaded");
            }

            var sceneInstance = loadedScenes.GetValueOrDefault(identifier);

            // AddTo(this)されると途中でdisposableされてしまうのでIObservableで公開しない.
            UnloadScene(sceneInstance).Subscribe().AddTo(Disposable);
        }

        private IObservable<Unit> UnloadScene(SceneInstance sceneInstance)
        {
            if (sceneInstance == null) { return Observable.ReturnUnit(); }

            if (!sceneInstance.Identifier.HasValue) { return Observable.ReturnUnit(); }
            
            var identifier = sceneInstance.Identifier.Value;

            var observable = unloadingScenes.GetValueOrDefault(identifier);

            if (observable == null)
            {
                observable = Observable.Defer(() => ObservableEx.FromUniTask(cancelToken => UnloadSceneCore(sceneInstance, cancelToken))
                    .Do(_ => unloadingScenes.Remove(identifier)))
                    .Share();

                unloadingScenes.Add(identifier, observable);
            }

            return observable;
        }

        private async UniTask UnloadSceneCore(SceneInstance sceneInstance, CancellationToken cancelToken)
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

			var sceneBase = sceneInstance.Instance as SceneBase;

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

				while (!op.isDone)
				{
					if (cancelToken.IsCancellationRequested){ break; }

					await UniTask.NextFrame(cancelToken);
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

            if (onUnloadSceneComplete != null)
            {
                onUnloadSceneComplete.OnNext(sceneInstance);
            }
        }

        public IObservable<SceneInstance> OnUnloadSceneAsObservable()
        {
            return onUnloadScene ?? (onUnloadScene = new Subject<SceneInstance>());
        }

        public IObservable<SceneInstance> OnUnloadSceneCompleteAsObservable()
        {
            return onUnloadSceneComplete ?? (onUnloadSceneComplete = new Subject<SceneInstance>());
        }

        public IObservable<Unit> OnUnloadErrorAsObservable()
        {
            return onUnloadError ?? (onUnloadError = new Subject<Unit>());
        }

        #endregion

        #region Scene Preload

        /// <summary> 事前読み込み要求. </summary>
        public void RequestPreLoad(Scenes[] targetScenes)
        {
            if (preLoadDisposable == null)
            {
                preLoadDisposable = new LifetimeDisposable();
            }

            PreLoadScene(targetScenes).Subscribe().AddTo(preLoadDisposable.Disposable);
        }

        private IObservable<Unit> PreLoadScene(Scenes[] targetScenes)
        {
            if (targetScenes.IsEmpty()) { return Observable.ReturnUnit(); }

            var builder = new StringBuilder();

            var observers = new List<IObservable<Unit>>();

            foreach (var scene in targetScenes)
            {
                // キャッシュ済みのシーンがある場合はプリロードしない.
                if (cacheScenes.Any(x => x.Identifier == scene)) { continue; }

                var observer = Observable.Defer(() => ObservableEx.FromUniTask(cancelToken => PreLoadCore(scene, builder, cancelToken)));

                observers.Add(observer);
            }

            if (observers.IsEmpty()) { return Observable.ReturnUnit(); }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            return observers.WhenAll()
                .Do(_ =>
                    {
                        sw.Stop();

                        var time = sw.Elapsed.TotalMilliseconds;
                        var detail = builder.ToString();

                        var message = string.Format("PreLoad Complete ({0:F2}ms)\n\n{1}", time, detail);

                        UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
                    })
                .AsUnitObservable();
        }

        private async UniTask PreLoadCore(Scenes targetScene, StringBuilder builder, CancellationToken cancelToken)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				await LoadScene(targetScene, LoadSceneMode.Additive).ToUniTask(cancellationToken: cancelToken);
			}
			catch (OperationCanceledException) 
			{
				return;
			}
			catch (Exception e)
			{
				OnLoadError(e, targetScene);
			}

			sw.Stop();

			var time = sw.Elapsed.TotalMilliseconds;

			builder.AppendLine(string.Format("{0} ({1:F2}ms)", targetScene, time));
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
        private void UnloadCacheScene(SceneInstance sceneInstance)
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
                    loadedScenes.Remove(sceneInstance.Identifier.Value);
                }

                UnloadScene(sceneInstance).Subscribe().AddTo(Disposable);
            }
        }

        #endregion

        /// <summary> シーンが展開済みか </summary>
        public bool IsSceneLoaded(Scenes identifier)
        {
            return loadedScenes.ContainsKey(identifier);
        }

		/// <summary> シーンを取得 </summary>
		public SceneInstance GetSceneInstance(Scenes identifier)
		{
			return loadedScenes.GetValueOrDefault(identifier);
		}

		private ISceneBase FindSceneObject(UnityEngine.SceneManagement.Scene scene)
        {
            ISceneBase sceneBase = null;

            if (!scene.isLoaded || !scene.IsValid()) { return null; }

            var rootObjects = scene.GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                sceneBase = UnityUtility.FindObjectOfInterface<ISceneBase>(rootObject);

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

		//====== Force Transition ======

		public IObservable<ISceneArgument> OnForceTransitionAsObservable()
		{
			return onForceTransition ?? (onForceTransition = new Subject<ISceneArgument>());
		}

        //====== Prepare Scene ======

        public IObservable<SceneInstance> OnPrepareAsObservable()
        {
            return onPrepare ?? (onPrepare = new Subject<SceneInstance>());
        }

        public IObservable<SceneInstance> OnPrepareCompleteAsObservable()
        {
            return onPrepareComplete ?? (onPrepareComplete = new Subject<SceneInstance>());
        }

        //====== Enter Scene ======

        public IObservable<SceneInstance> OnEnterAsObservable()
        {
            return onEnter ?? (onEnter = new Subject<SceneInstance>());
        }

        public IObservable<SceneInstance> OnEnterCompleteAsObservable()
        {
            return onEnterComplete ?? (onEnterComplete = new Subject<SceneInstance>());
        }

        //====== Leave Scene ======

        public IObservable<SceneInstance> OnLeaveAsObservable()
        {
            return onLeave ?? (onLeave = new Subject<SceneInstance>());
        }

        public IObservable<SceneInstance> OnLeaveCompleteAsObservable()
        {
            return onLeaveComplete ?? (onLeaveComplete = new Subject<SceneInstance>());
        }

        protected abstract UniTask TransitionStart<TArgument>(TArgument sceneArgument, bool isSceneBack) where TArgument : ISceneArgument;

        protected abstract UniTask TransitionFinish<TArgument>(TArgument sceneArgument, bool isSceneBack) where TArgument : ISceneArgument;
    }
}
