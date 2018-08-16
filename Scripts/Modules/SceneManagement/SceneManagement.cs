
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Constants;
using Modules.Devkit;
using Modules.SceneManagement.Diagnostics;

namespace Modules.SceneManagement
{
    public abstract partial class SceneManagement<T> : Singleton<T> where T : SceneManagement<T>, new()
    {
        //----- params -----

        // 起動シーン用の空引数.
        private class BootSceneArgument : ISceneArgument
        {
            public Scenes? Identifier { get; private set; }

            public BootSceneArgument(Scenes? identifier)
            {
                Identifier = identifier;
            }
        }

        private readonly string ConsoleEventName = "Scene";
        private readonly Color ConsoleEventColor = new Color(135, 206, 235);

        //----- field -----

        private IDisposable transitionDisposable = null;

        private SceneInfo current = null;
        private ISceneArgument currentSceneArgument = null;
        private List<ISceneArgument> history = null;

        private List<SceneInfo> additiveScene = null;

        private Subject<ISceneArgument> onPrepare = null;
        private Subject<ISceneArgument> onPrepareComplete = null;

        private Subject<ISceneArgument> onEnter = null;
        private Subject<ISceneArgument> onEnterComplete = null;

        private Subject<ISceneArgument> onLeave = null;
        private Subject<ISceneArgument> onLeaveComplete = null;

        private Subject<SceneInfo> onLoadScene = null;
        private Subject<SceneInfo> onLoadSceneComplete = null;
        private Subject<Unit> onLoadError = null;

        private Subject<SceneInfo> onUnloadScene = null;
        private Subject<SceneInfo> onUnloadSceneComplete = null;
        private Subject<Unit> onUnloadError = null;

        //----- property -----

        /// <summary> 現在のシーン情報 </summary>
        public SceneInfo Current { get { return current; } }

        /// <summary> シーン加算で読み込まれているシーン情報 </summary>
        public SceneInfo[] AdditiveScene { get { return additiveScene.ToArray(); } }

        /// <summary> 遷移中か </summary>
        public bool IsTransition { get { return transitionDisposable != null; } }

        /// <summary> 遷移先のシーン </summary>
        public Scenes? TransitionTarget { get; private set; }

        /// <summary> シーンを読み込む為の定義情報 </summary>
        protected abstract Dictionary<Scenes, string> ScenePaths { get; }

        //----- method -----

        protected SceneManagement()
        {
            history = new List<ISceneArgument>();
            additiveScene = new List<SceneInfo>();
            waitEntityIds = new HashSet<int>();
        }

        public IObservable<Unit> WaitBootSceneReady()
        {
            var observers = new List<IObservable<Unit>>();

            var sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                observers.Add(Observable.Defer(() => Observable.EveryUpdate().SkipWhile(_ => !scene.isLoaded).First().AsUnitObservable()));
            }

            return observers.WhenAll().AsUnitObservable();
        }

        public IObservable<Unit> RegisterCurrentScene()
        {
            return Observable.FromCoroutine(() => RegisterCurrentSceneCore());
        }

        private IEnumerator RegisterCurrentSceneCore()
        {
            if (current != null) { yield break; }

            var scene = SceneManager.GetSceneAt(0);

            while (!scene.isLoaded) { yield return null; }

            var definition = ScenePaths.FirstOrDefault(x => x.Value == scene.path);
            var identifier = definition.Equals(default(KeyValuePair<Scenes, string>)) ? null : (Scenes?)definition.Key;

            var sceneInstance = UnityUtility.FindObjectsOfInterface<ISceneBase>().FirstOrDefault();

            CollectUniqueComponents(scene.GetRootGameObjects());

            if (sceneInstance != null)
            {
                current = new SceneInfo(identifier, sceneInstance, LoadSceneMode.Single, scene);

                // 起動シーンは引数なしで遷移してきたという扱い.
                history.Add(new BootSceneArgument(identifier));
            }

            if (current == null || current.Instance == null)
            {
                Debug.LogError("Current scene not found.");

                yield break;
            }

            yield return OnRegisterCurrentScene(current).ToYieldInstruction();

            yield return current.Instance.PrepareAsync(false).ToYieldInstruction();

            current.Instance.Enter(false);
        }

        /// <summary> 初期シーン登録時のイベント </summary>
        protected virtual IObservable<Unit> OnRegisterCurrentScene(SceneInfo currentInfo) { return Observable.ReturnUnit(); }

        /// <summary>
        /// シーン遷移.
        /// </summary>
        /// <typeparam name="TArgument"></typeparam>
        /// <param name="sceneArgument"></param>
        /// <param name="registerHistory"></param>
        /// <returns></returns>
        public void Transition<TArgument>(TArgument sceneArgument, bool registerHistory = false) where TArgument : ISceneArgument
        {
            // 遷移中は遷移不可.
            if (TransitionTarget != null) { return; }

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            transitionDisposable = Observable.FromCoroutine(() => TransitionCore(sceneArgument, false, registerHistory))
                .Subscribe(_ => transitionDisposable = null)
                .AddTo(Disposable);
        }

        /// <summary>
        /// 強制シーン遷移.
        /// </summary>
        /// <typeparam name="TArgument"></typeparam>
        /// <param name="sceneArgument"></param>
        /// <param name="registerHistory"></param>
        /// <returns></returns>
        public void ForceTransition<TArgument>(TArgument sceneArgument, bool registerHistory = false) where TArgument : ISceneArgument
        {
            TransitionCancel();

            // ※ 呼び出し元でAddTo(this)されるとシーン遷移中にdisposableされてしまうのでIObservableで公開しない.
            transitionDisposable = Observable.FromCoroutine(() => TransitionCore(sceneArgument, false, registerHistory))
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
                transitionDisposable = Observable.FromCoroutine(() => TransitionCore(argument, true, false))
                    .Subscribe(_ => transitionDisposable = null)
                    .AddTo(Disposable);
            }
        }

        /// <summary>
        /// シーン遷移履歴をクリア.
        /// </summary>
        public void ClearTransitionHistory()
        {
            // 現在のシーンの情報は残す.
            var currentEntity = history.Last();

            history.Clear();

            history.Add(currentEntity);
        }

        /// <summary>
        /// シーン遷移の引数履歴取得.
        /// </summary>
        public ISceneArgument[] GetArgumentHistory()
        {
            return history.ToArray();
        }

        private IEnumerator TransitionCore<TArgument>(TArgument sceneArgument, bool isSceneBack, bool registerHistory) where TArgument : ISceneArgument
        {
            if (!sceneArgument.Identifier.HasValue) { yield break; }

            TransitionTarget = sceneArgument.Identifier;

            var prevSceneArgument = currentSceneArgument;

            currentSceneArgument = sceneArgument;

            var diagnostics = new TimeDiagnostics();

            var prev = current;

            // 現在のシーンを履歴に残さない場合は、既に登録済みの現在のシーン(history[要素数 - 1])を外す.
            if (!registerHistory && history.Any())
            {
                history.RemoveAt(history.Count - 1);
            }

            //====== Begin Transition ======

            diagnostics.Begin(TimeDiagnostics.Measure.Total);

            yield return TransitionStart(currentSceneArgument).ToYieldInstruction();

            if (current != null)
            {
                //====== Scene Leave ======

                diagnostics.Begin(TimeDiagnostics.Measure.Leave);

                // Leave通知.
                if (onLeave != null)
                {
                    onLeave.OnNext(prevSceneArgument);
                }

                // 現在のシーンの終了処理を実行.
                yield return current.Instance.LeaveAsync().ToYieldInstruction();

                // PlayerPrefsを保存.
                PlayerPrefs.Save();

                // Leave終了通知.
                if (onLeaveComplete != null)
                {
                    onLeaveComplete.OnNext(prevSceneArgument);
                }

                diagnostics.Finish(TimeDiagnostics.Measure.Leave);
            }

            //====== Load Next Scene ======

            diagnostics.Begin(TimeDiagnostics.Measure.Load);

            var identifier = sceneArgument.Identifier.Value;

            var loadYield = LoadScene(identifier, LoadSceneMode.Single).ToYieldInstruction();

            yield return loadYield;

            if (!loadYield.HasResult) { yield break; }

            var sceneInfo = loadYield.Result;

            SetSceneActive(sceneInfo.GetScene());

            var scene = sceneInfo.GetScene();

            if (!scene.HasValue)
            {
                Debug.LogErrorFormat("[ {0} ] : Scene情報の取得に失敗しました.", identifier);

                yield break;
            }

            if (sceneInfo.Instance == null)
            {
                Debug.LogErrorFormat("[ {0} ] : SceneBase継承クラスが存在しません.", scene.Value.path);

                yield break;
            }

            // 前のシーンからの引数を設定.
            sceneInfo.Instance.SetArgument(sceneArgument);

            // シーンを有効化.
            sceneInfo.Enable();

            // 次のシーンを現在のシーンとして登録.
            current = new SceneInfo(currentSceneArgument.Identifier, sceneInfo.Instance, sceneInfo.Mode, scene.Value);

            // 次のシーンを履歴に登録.
            // シーン引数を保存する為遷移時に引数と一緒に履歴登録する為、履歴の最後尾は現在のシーンになる.
            if (current.Instance != null)
            {
                history.Add(currentSceneArgument);
            }

            // シーン読み込み後にAwake、Startが終わるのを待つ為1フレーム後に処理を再開.
            yield return null;

            diagnostics.Finish(TimeDiagnostics.Measure.Load);

            //====== Scene Prepare ======

            diagnostics.Begin(TimeDiagnostics.Measure.Prepare);

            // Prepar通知.
            if (onPrepare != null)
            {
                onPrepare.OnNext(currentSceneArgument);
            }

            // 次のシーンの準備処理実行.
            if (current.Instance != null)
            {
                yield return current.Instance.PrepareAsync(isSceneBack).ToYieldInstruction();
            }

            // Prepar終了通知.
            if (onPrepareComplete != null)
            {
                onPrepareComplete.OnNext(currentSceneArgument);
            }

            diagnostics.Finish(TimeDiagnostics.Measure.Prepare);

            //====== Scene Wait ======

            // メモリ解放.
            yield return CleanUp().ToYieldInstruction();

            // 外部処理待機.
            yield return Observable.FromCoroutine(() => TransitionWait()).ToYieldInstruction();

            // シーン遷移終了.
            yield return TransitionFinish(currentSceneArgument).ToYieldInstruction();

            //====== End Transition ======

            TransitionTarget = null;

            //====== Report ======

            diagnostics.Finish(TimeDiagnostics.Measure.Total);

            var prevScene = prev.Identifier;
            var nextScene = current.Identifier;

            var total = diagnostics.GetTime(TimeDiagnostics.Measure.Total);
            var detail = diagnostics.BuildDetailText();

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "{0} → {1} ({2:F2}ms)\n\n{3}", prevScene, nextScene, total, detail);

            //====== Scene Enter ======

            // Enter通知.
            if (onEnter != null)
            {
                onEnter.OnNext(currentSceneArgument);
            }

            // 次のシーンの開始処理実行.
            if (current.Instance != null)
            {
                current.Instance.Enter(isSceneBack);
            }

            // Enter終了通知.
            if (onEnterComplete != null)
            {
                onEnterComplete.OnNext(currentSceneArgument);
            }
        }

        #region Scene Additive

        /// <summary>
        /// シーンを追加で読み込み.
        /// <para> Prepar, Enter, Leaveは自動で呼び出されないので自分で制御する </para>
        /// </summary>
        public IObservable<SceneInfo> Append<TArgument>(TArgument sceneArgument, bool activeOnLoad = true) where TArgument : ISceneArgument
        {
            return Observable.FromCoroutine<SceneInfo>(observer => AppendCore(observer, sceneArgument.Identifier, activeOnLoad))
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
        /// <para> Prepar, Enter, Leaveは自動で呼び出されないので自分で制御する </para>
        /// </summary>
        public IObservable<SceneInfo> Append(Scenes identifier, bool activeOnLoad = true)
        {
            return Observable.FromCoroutine<SceneInfo>(observer => AppendCore(observer, identifier, activeOnLoad));
        }

        private IEnumerator AppendCore(IObserver<SceneInfo> observer, Scenes? identifier, bool activeOnLoad)
        {
            if (!identifier.HasValue) { yield break; }

            SceneInfo sceneInfo = null;

            var diagnostics = new TimeDiagnostics();

            diagnostics.Begin(TimeDiagnostics.Measure.Total);

            var loadYield = LoadScene(identifier.Value, LoadSceneMode.Additive).ToYieldInstruction();

            yield return loadYield;

            if (loadYield.HasResult)
            {
                sceneInfo = loadYield.Result;

                diagnostics.Finish(TimeDiagnostics.Measure.Total);

                var additiveTime = diagnostics.GetTime(TimeDiagnostics.Measure.Total);

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "{0} ({1}ms)(Additive)", identifier.Value, additiveTime);
            }

            if (activeOnLoad)
            {
                sceneInfo.Enable();
            }

            observer.OnNext(sceneInfo);
            observer.OnCompleted();
        }

        /// <summary> 加算シーンをアンロード </summary>
        public IObservable<Unit> Remove(SceneInfo sceneInfo)
        {
            return UnloadScene(sceneInfo);
        }

        #endregion

        #region Scene Load

        private IObservable<SceneInfo> LoadScene(Scenes identifier, LoadSceneMode mode)
        {
            return Observable.FromCoroutine<SceneInfo>(observer => LoadSceneCore(observer, identifier, mode));
        }

        private IEnumerator LoadSceneCore(IObserver<SceneInfo> observer, Scenes identifier, LoadSceneMode mode)
        {
            SceneInfo sceneInfo = null;

            var scenePath = ScenePaths.GetValueOrDefault(identifier);

            UnityAction<Scene, LoadSceneMode> sceneLoaded = (s, m) =>
            {
                if (s.IsValid())
                {
                    sceneInfo = new SceneInfo(identifier, FindSceneObject(s), m, s);

                    switch (m)
                    {
                        case LoadSceneMode.Single:
                            additiveScene.Clear();
                            break;

                        case LoadSceneMode.Additive:
                            additiveScene.Add(sceneInfo);
                            break;
                    }

                    if (s.IsValid())
                    {
                        var rootObjects = s.GetRootGameObjects();

                        // UniqueComponentsを回収.
                        CollectUniqueComponents(rootObjects);
                    }

                    // 初期状態は非アクティブ.
                    sceneInfo.Disable();

                    if (onLoadScene != null)
                    {
                        onLoadScene.OnNext(sceneInfo);
                    }
                }
            };

            SetEnabledForCapturedComponents(false);

            SceneManager.sceneLoaded += sceneLoaded;

            AsyncOperation op = null;

            try
            {
                op = SceneManager.LoadSceneAsync(scenePath, mode);
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                SceneManager.sceneLoaded -= sceneLoaded;

                if (onLoadError != null)
                {
                    onLoadError.OnNext(Unit.Default);
                }

                observer.OnError(e);

                yield break;
            }

            while (!op.isDone)
            {
                yield return op;
            }

            SceneManager.sceneLoaded -= sceneLoaded;

            SetEnabledForCapturedComponents(true);

            if (sceneInfo != null)
            {
                if (onLoadSceneComplete != null)
                {
                    onLoadSceneComplete.OnNext(sceneInfo);
                }

                var scene = sceneInfo.GetScene();

                if (scene.HasValue)
                {
                    var rootObjects = scene.Value.GetRootGameObjects();

                    foreach (var rootObject in rootObjects)
                    {
                        var targets = UnityUtility.FindObjectsOfInterface<ISceneEvent>(rootObject);

                        foreach (var target in targets)
                        {
                            yield return target.OnLoadSceneAsObservable().ToYieldInstruction();
                        }
                    }
                }
            }

            observer.OnNext(sceneInfo);
            observer.OnCompleted();
        }

        public IObservable<SceneInfo> OnLoadSceneAsObservable()
        {
            return onLoadScene ?? (onLoadScene = new Subject<SceneInfo>());
        }

        public IObservable<SceneInfo> OnLoadSceneCompleteAsObservable()
        {
            return onLoadSceneComplete ?? (onLoadSceneComplete = new Subject<SceneInfo>());
        }

        public IObservable<Unit> OnLoadErrorAsObservable()
        {
            return onLoadError ?? (onLoadError = new Subject<Unit>());
        }

        #endregion

        #region Scene Unload

        private IObservable<Unit> UnloadScene(SceneInfo sceneInfo)
        {
            if (sceneInfo == null) { return Observable.ReturnUnit(); }

            if (sceneInfo.Mode != LoadSceneMode.Additive)
            {
                Debug.LogError("Only the added scenes can be unloaded.");
                return Observable.ReturnUnit();
            }

            return Observable.FromCoroutine(() => UnloadSceneCore(sceneInfo));
        }

        private IEnumerator UnloadSceneCore(SceneInfo sceneInfo)
        {
            var scene = sceneInfo.GetScene();

            if (!scene.HasValue) { yield break; }

            UnityAction<Scene> sceneUnloaded = (s) =>
            {
                switch (sceneInfo.Mode)
                {
                    case LoadSceneMode.Additive:
                        additiveScene.Remove(sceneInfo);
                        break;
                }

                if (onUnloadScene != null)
                {
                    onUnloadScene.OnNext(sceneInfo);
                }
            };

            var rootObjects = scene.Value.GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                var targets = UnityUtility.FindObjectsOfInterface<ISceneEvent>(rootObject);

                foreach (var target in targets)
                {
                    yield return target.OnUnloadSceneAsObservable().ToYieldInstruction();
                }
            }

            AsyncOperation op = null;

            try
            {
                SceneManager.sceneUnloaded += sceneUnloaded;

                op = SceneManager.UnloadSceneAsync(scene.Value);
            }
            catch (Exception e)
            {
                SceneManager.sceneUnloaded -= sceneUnloaded;

                Debug.LogException(e);

                if (onUnloadError != null)
                {
                    onUnloadError.OnNext(Unit.Default);
                }

                yield break;
            }

            while (!op.isDone)
            {
                yield return op;
            }

            SceneManager.sceneUnloaded -= sceneUnloaded;

            if (onUnloadSceneComplete != null)
            {
                onUnloadSceneComplete.OnNext(sceneInfo);
            }
        }

        public IObservable<SceneInfo> OnUnloadSceneAsObservable()
        {
            return onUnloadScene ?? (onUnloadScene = new Subject<SceneInfo>());
        }

        public IObservable<SceneInfo> OnUnloadSceneCompleteAsObservable()
        {
            return onUnloadSceneComplete ?? (onUnloadSceneComplete = new Subject<SceneInfo>());
        }

        public IObservable<Unit> OnUnloadErrorAsObservable()
        {
            return onUnloadError ?? (onUnloadError = new Subject<Unit>());
        }

        #endregion

        /// <summary> シーンが展開済みか </summary>
        public bool IsSceneLoaded(Scenes identifier)
        {
            var path = ScenePaths.GetValueOrDefault(identifier);

            if (string.IsNullOrEmpty(path)) { return false; }

            var scene = current.GetScene();

            if (current != null && scene.HasValue)
            {
                if (scene.Value.path == path) { return true; }
            }

            return additiveScene
                .Select(x => x.GetScene())
                .Where(x => x != null)
                .Any(x => x.Value.path == path);
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

        private static IEnumerator CleanUp()
        {
            yield return Resources.UnloadUnusedAssets();

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

        protected abstract IObservable<Unit> TransitionStart<TArgument>(TArgument sceneArgument) where TArgument : ISceneArgument;

        protected abstract IObservable<Unit> TransitionFinish<TArgument>(TArgument sceneArgument) where TArgument : ISceneArgument;
    }
}