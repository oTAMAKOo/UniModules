
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
using Modules.SceneManagement.Diagnostics;

namespace Modules.SceneManagement
{
    public abstract partial class SceneManagement<T> : Singleton<T> where T : SceneManagement<T>
    {
        //----- params -----

        public readonly string ConsoleEventName = "Scene";
        public readonly Color ConsoleEventColor = new Color(0.4f, 1f, 0.4f);

        //----- field -----

        private IDisposable transitionDisposable = null;

        private Dictionary<Scenes, SceneInstance> loadedScenes = null;
        private FixedQueue<SceneInstance> cacheScenes = null;

        private Dictionary<Scenes, IObservable<SceneInstance>> loadingScenes = null;
        private Dictionary<Scenes, IObservable<Unit>> unloadingScenes = null;

        private SceneInstance currentScene = null;
        private ISceneArgument currentSceneArgument = null;

        private List<SceneInstance> appendSceneInstances = null;

        private List<ISceneArgument> history = null;

        private LifetimeDisposable preLoadDisposable = null;

        private Subject<ISceneArgument> onPrepare = null;
        private Subject<ISceneArgument> onPrepareComplete = null;

        private Subject<ISceneArgument> onEnter = null;
        private Subject<ISceneArgument> onEnterComplete = null;

        private Subject<ISceneArgument> onLeave = null;
        private Subject<ISceneArgument> onLeaveComplete = null;

        private Subject<SceneInstance> onLoadScene = null;
        private Subject<SceneInstance> onLoadSceneComplete = null;
        private Subject<Unit> onLoadError = null;

        private Subject<SceneInstance> onUnloadScene = null;
        private Subject<SceneInstance> onUnloadSceneComplete = null;
        private Subject<Unit> onUnloadError = null;

        //----- property -----

        /// <summary> 現在のシーン情報 </summary>
        public SceneInstance Current { get { return currentScene; } }

        /// <summary> 遷移中か </summary>
        public bool IsTransition { get { return transitionDisposable != null; } }

        /// <summary> 遷移先のシーン </summary>
        public Scenes? TransitionTarget { get; private set; }

        /// <summary> キャッシュするシーン数 </summary>
        protected virtual int CacheSize { get { return 3; } }

        /// <summary> シーンを読み込む為の定義情報 </summary>
        protected abstract Dictionary<Scenes, string> ScenePaths { get; }

        //----- method -----

        protected SceneManagement() { }

        protected override void OnCreate()
        {
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

			// ISceneEvent.

			var tasks = new List<UniTask>();

			var rootObjects = scene.GetRootGameObjects();

			foreach (var rootObject in rootObjects)
			{
				var targets = UnityUtility.FindObjectsOfInterface<ISceneEvent>(rootObject);

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
                onPrepare.OnNext(sceneArgument);
            }

			await currentScene.Instance.Prepare(false);

            if (onPrepareComplete != null)
            {
                onPrepareComplete.OnNext(sceneArgument);
            }

			// Enter.

			if (onEnter != null)
            {
                onEnter.OnNext(sceneArgument);
            }

            currentScene.Instance.Enter(false);

            if (onEnterComplete != null)
            {
                onEnterComplete.OnNext(sceneArgument);
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

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            transitionDisposable = Observable.FromUniTask(cancelToken => TransitionCore(sceneArgument, LoadSceneMode.Additive, false, registerHistory, cancelToken))
                .Subscribe(_ => transitionDisposable = null)
                .AddTo(Disposable);
        }

        /// <summary> 強制シーン遷移. </summary>
        public void ForceTransition<TArgument>(TArgument sceneArgument, bool registerHistory = false) where TArgument : ISceneArgument
        {
            TransitionCancel();

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            transitionDisposable = Observable.FromUniTask(cancelToken => TransitionCore(sceneArgument, LoadSceneMode.Single, false, registerHistory, cancelToken))
                .Subscribe(_ => transitionDisposable = null)
                .AddTo(Disposable);
        }

        /// <summary> シーン再読み込み. </summary>
        public void Reload()
        {
            // 遷移中は遷移不可.
            if (IsTransition) { return; }

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            transitionDisposable = Observable.FromUniTask(cancelToken => TransitionCore(currentSceneArgument, LoadSceneMode.Additive, false, false, cancelToken))
                .Subscribe(_ => transitionDisposable = null)
                .AddTo(Disposable);
        }

        /// <summary>
        /// シーン遷移を中止.
        /// </summary>
        private void TransitionCancel()
        {
            if (transitionDisposable != null)
            {
                transitionDisposable.Dispose();
                transitionDisposable = null;
            }

            TransitionTarget = null;
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
                transitionDisposable = Observable.FromUniTask(cancelToken => TransitionCore(argument, LoadSceneMode.Additive, true, false, cancelToken))
                    .Subscribe(_ => transitionDisposable = null)
                    .AddTo(Disposable);
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

        /// <summary>
        /// シーン遷移の引数履歴取得.
        /// </summary>
        public ISceneArgument[] GetArgumentHistory()
        {
            return history.ToArray();
        }

        private async UniTask TransitionCore<TArgument>(TArgument argument, LoadSceneMode mode, bool isSceneBack, bool registerHistory, CancellationToken cancelToken) 
			where TArgument : ISceneArgument
        {
            if (!argument.Identifier.HasValue) { return; }

            // プリロード停止.
            if (preLoadDisposable != null)
            {
                preLoadDisposable.Dispose();
                preLoadDisposable = null;
            }

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
				await TransitionStart(currentSceneArgument).AttachExternalCancellation(cancelToken);
			}
			catch (OperationCanceledException) 
			{
				/* Canceled */
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
                    onLeave.OnNext(prevSceneArgument);
                }

                // 現在のシーンの終了処理を実行.
				try
				{
					await prev.Instance.Leave().AttachExternalCancellation(cancelToken);
				}
				catch (OperationCanceledException) 
				{
					/* Canceled */
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
                    onLeaveComplete.OnNext(prevSceneArgument);
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
						/* Canceled */
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

            //====== Load Next Scene ======

            diagnostics.Begin(TimeDiagnostics.Measure.Load);
            
            // 次のシーンを読み込み.
            var identifier = argument.Identifier.Value;

            var sceneInfo = loadedScenes.GetValueOrDefault(identifier);

            if (sceneInfo == null)
            {
				try
				{
					var awaiter = LoadScene(identifier, mode).GetAwaiter(cancelToken);

					while (!awaiter.IsCompleted)
					{
						await UniTask.NextFrame(cancelToken);
					}

					sceneInfo = awaiter.GetResult();
				}
				catch (OperationCanceledException) 
				{
					/* Canceled */
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
                Debug.LogErrorFormat("[ {0} ] : Scene情報の取得に失敗しました.", identifier);

				return;
            }

            if (sceneInfo.Instance == null)
            {
                Debug.LogErrorFormat("[ {0} ] : SceneBase継承クラスが存在しません.", scene.Value.path);

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

            diagnostics.Finish(TimeDiagnostics.Measure.Load);

            //====== Scene Prepare ======

            diagnostics.Begin(TimeDiagnostics.Measure.Prepare);

            // Prepare通知.
            if (onPrepare != null)
            {
                onPrepare.OnNext(currentSceneArgument);
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
					/* Canceled */
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

            // Prepare終了通知.
            if (onPrepareComplete != null)
            {
                onPrepareComplete.OnNext(currentSceneArgument);
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
					/* Canceled */
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
				/* Canceled */
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
				/* Canceled */
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
				await TransitionFinish(currentSceneArgument).AttachExternalCancellation(cancelToken);
			}
			catch (OperationCanceledException) 
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			//====== Scene Enter ======

            // Enter通知.
            if (onEnter != null)
            {
                onEnter.OnNext(currentSceneArgument);
            }

            // 次のシーンの開始処理実行.
            if (currentScene.Instance != null)
            {
                currentScene.Instance.Enter(isSceneBack);
            }

            // Enter終了通知.
            if (onEnterComplete != null)
            {
                onEnterComplete.OnNext(currentSceneArgument);
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

        #region Scene Additive

        /// <summary>
        /// シーンを追加で読み込み.
        /// <para> Prepare, Enter, Leaveは自動で呼び出されないので自分で制御する </para>
        /// </summary>
        public IObservable<SceneInstance> Append<TArgument>(TArgument sceneArgument, bool activeOnLoad = true) where TArgument : ISceneArgument
        {
            return Observable.FromUniTask(cancelToken => AppendCore(sceneArgument.Identifier, activeOnLoad, cancelToken))
                .Do(x =>
                {
                    // シーンルート引数設定.
                    if (x != null && x.Instance != null)
                    {
                        x.Instance.SetArgument(sceneArgument);
                    }
                });
        }

        /// <summary>
        /// シーンを追加で読み込み.
        /// <para> Prepare, Enter, Leaveは自動で呼び出されないので自分で制御する </para>
        /// </summary>
        public IObservable<SceneInstance> Append(Scenes identifier, bool activeOnLoad = true)
        {
            return Observable.FromUniTask(cancelToken => AppendCore(identifier, activeOnLoad, cancelToken));
        }

        private async UniTask<SceneInstance> AppendCore(Scenes? identifier, bool activeOnLoad, CancellationToken cancelToken)
        {
            if (!identifier.HasValue) { return null; }

            SceneInstance sceneInstance = null;

            var diagnostics = new TimeDiagnostics();

            diagnostics.Begin(TimeDiagnostics.Measure.Append);

			try
			{
				var awaiter = LoadScene(identifier.Value, LoadSceneMode.Additive).GetAwaiter(cancelToken);

				while (!awaiter.IsCompleted)
				{
					await UniTask.NextFrame(cancelToken);
				}

				sceneInstance = awaiter.GetResult();
			}
			catch (OperationCanceledException) 
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				OnLoadError(e, identifier);
			}

			if (sceneInstance != null)
            {
                appendSceneInstances.Add(sceneInstance);

                diagnostics.Finish(TimeDiagnostics.Measure.Append);

                var additiveTime = diagnostics.GetTime(TimeDiagnostics.Measure.Append);

                var message = string.Format("{0} ({1:F2}ms)(Additive)", identifier.Value, additiveTime);

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

                if (activeOnLoad)
                {
                    sceneInstance.Enable();
                }
            }

			return sceneInstance;
		}

        /// <summary> 加算シーンをアンロード </summary>
        public void UnloadAppendScene(ISceneBase scene, bool deactivateSceneObjects = true)
        {
            var sceneInstance = appendSceneInstances.FirstOrDefault(x => x.Instance == scene);

            if (sceneInstance == null) { return; }

            UnloadAppendScene(sceneInstance, deactivateSceneObjects);
        }

        /// <summary> 加算シーンをアンロード </summary>
        public void UnloadAppendScene(SceneInstance sceneInstance, bool deactivateSceneObjects = true)
        {
            if (!appendSceneInstances.Contains(sceneInstance)){ return; }

            if (deactivateSceneObjects)
            {
                sceneInstance.Disable();
            }

            appendSceneInstances.Remove(sceneInstance);

            // AddTo(this)されると途中でdisposableされてしまうのでIObservableで公開しない.
            UnloadScene(sceneInstance).Subscribe().AddTo(Disposable);
        }

        #endregion

        #region Scene Load
        
        private IObservable<SceneInstance> LoadScene(Scenes identifier, LoadSceneMode mode)
        {
            var observable = loadingScenes.GetValueOrDefault(identifier);

            if (observable == null)
            {
                observable = Observable.Defer(() => Observable.FromUniTask(cancelToken => LoadSceneCore(identifier, mode, cancelToken))
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

                UnityAction<Scene, LoadSceneMode> sceneLoaded = (s, m) =>
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
						await UniTask.NextFrame(cancelToken);
					}
                }
				catch (OperationCanceledException) 
				{
					/* Canceled */
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

                    if (onLoadSceneComplete != null)
                    {
                        onLoadSceneComplete.OnNext(sceneInstance);
                    }

                    // ISceneEvent発行.
                    foreach (var rootObject in rootObjects)
                    {
                        var targets = UnityUtility.FindObjectsOfInterface<ISceneEvent>(rootObject);

                        foreach (var target in targets)
                        {
							try
							{
								await target.OnLoadScene().AttachExternalCancellation(cancelToken);
							}
							catch (OperationCanceledException) 
							{
								/* Canceled */
							}
							catch (Exception e)
							{
								Debug.LogException(e);
							}
						}
                    }
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
						/* Canceled */
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
                observable = Observable.Defer(() => Observable.FromUniTask(cancelToken => UnloadSceneCore(sceneInstance, cancelToken))
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

            UnityAction<Scene> sceneUnloaded = s =>
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

            var rootObjects = scene.Value.GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                var targets = UnityUtility.FindObjectsOfInterface<ISceneEvent>(rootObject);

                foreach (var target in targets)
                {
					try
					{
						await target.OnUnloadScene().AttachExternalCancellation(cancelToken);
					}
					catch (OperationCanceledException) 
					{
						/* Canceled */
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
                }
            }

            AsyncOperation op = null;

            try
            {
                SceneManager.sceneUnloaded += sceneUnloaded;

                op = SceneManager.UnloadSceneAsync(scene.Value);
            }
			catch (OperationCanceledException) 
			{
				/* Canceled */
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

            while (!op.isDone)
            {
                await UniTask.NextFrame(cancelToken);
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

                var observer = Observable.Defer(() => Observable.FromUniTask(cancelToken => PreLoadCore(scene, builder, cancelToken)));

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
				var awaiter = LoadScene(targetScene, LoadSceneMode.Additive).GetAwaiter(cancelToken);

				while (!awaiter.IsCompleted)
				{
					await UniTask.NextFrame(cancelToken);
				}
			}
			catch (OperationCanceledException) 
			{
				/* Canceled */
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

        private ISceneBase FindSceneObject(Scene scene)
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

        private Scene[] GetAllScenes()
        {
            var scenes = new List<Scene>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                scenes.Add(SceneManager.GetSceneAt(i));
            }

            return scenes.ToArray();
        }

        private void SetSceneActive(Scene? scene)
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

        public IObservable<ISceneArgument> OnPrepareAsObservable()
        {
            return onPrepare ?? (onPrepare = new Subject<ISceneArgument>());
        }

        public IObservable<ISceneArgument> OnPrepareCompleteAsObservable()
        {
            return onPrepareComplete ?? (onPrepareComplete = new Subject<ISceneArgument>());
        }

        //====== Enter Scene ======

        public IObservable<ISceneArgument> OnEnterAsObservable()
        {
            return onEnter ?? (onEnter = new Subject<ISceneArgument>());
        }

        public IObservable<ISceneArgument> OnEnterCompleteAsObservable()
        {
            return onEnterComplete ?? (onEnterComplete = new Subject<ISceneArgument>());
        }

        //====== Leave Scene ======

        public IObservable<ISceneArgument> OnLeaveAsObservable()
        {
            return onLeave ?? (onLeave = new Subject<ISceneArgument>());
        }

        public IObservable<ISceneArgument> OnLeaveCompleteAsObservable()
        {
            return onLeaveComplete ?? (onLeaveComplete = new Subject<ISceneArgument>());
        }

        protected abstract UniTask TransitionStart<TArgument>(TArgument sceneArgument) where TArgument : ISceneArgument;

        protected abstract UniTask TransitionFinish<TArgument>(TArgument sceneArgument) where TArgument : ISceneArgument;
    }
}
