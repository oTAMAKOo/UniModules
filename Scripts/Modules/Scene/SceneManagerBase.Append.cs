
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;
using Modules.Scene.Diagnostics;

namespace Modules.Scene
{
    public abstract partial class SceneManagerBase<TInstance, TScenes>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        private void AppendModuleInitialize()
        {
            void OnUnloadSceneComple(SceneInstance<TScenes> sceneInstance)
            {
                if (sceneInstance == null){ return; }

                if (appendSceneInstances.Contains(sceneInstance))
                {
                    var message = $"[Unload Warning] {sceneInstance.Identifier} is force release.";

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message, LogType.Warning);

                    appendSceneInstances.Remove(sceneInstance);
                }
            }

            OnUnloadSceneCompleteAsObservable()
                .Subscribe(x => OnUnloadSceneComple(x))
                .AddTo(Disposable);
        }

        /// <summary>
        /// シーンを追加で読み込み.
        /// <para> Prepare, Enter, Leaveは自動で呼び出されないので自分で制御する </para>
        /// </summary>
        public IObservable<SceneInstance<TScenes>> Append<TArgument>(TArgument sceneArgument, bool activeOnLoad = true) 
            where TArgument : ISceneArgument<TScenes>
        {
            async UniTask SetArgumentCallback(SceneInstance<TScenes> sceneInstance)
            {
                if (sceneInstance == null){ return; }

                if (sceneInstance.Instance == null){ return; }

                await sceneInstance.Instance.SetArgument(sceneArgument);
            }

            return ObservableEx.FromUniTask(cancelToken => AppendCore(sceneArgument.Identifier, activeOnLoad, SetArgumentCallback, cancelToken));
        }

        /// <summary>
        /// シーンを追加で読み込み.
        /// <para> Prepare, Enter, Leaveは自動で呼び出されないので自分で制御する </para>
        /// </summary>
        public IObservable<SceneInstance<TScenes>> Append(TScenes identifier, bool activeOnLoad = true)
        {
            return ObservableEx.FromUniTask(cancelToken => AppendCore(identifier, activeOnLoad, null, cancelToken));
        }

        private async UniTask<SceneInstance<TScenes>> AppendCore(TScenes? identifier, bool activeOnLoad, Func<SceneInstance<TScenes>, 
            UniTask> setArgumentCallback, CancellationToken cancelToken)
        {
            if (!identifier.HasValue) { return null; }

            SceneInstance<TScenes> sceneInstance = null;

            var diagnostics = new TimeDiagnostics();

            diagnostics.Begin(TimeDiagnostics.Measure.Append);

            try
            {
                sceneInstance = await LoadScene(identifier.Value, LoadSceneMode.Additive).ToUniTask(cancellationToken: cancelToken);
            }
            catch (OperationCanceledException) 
            {
                return null;
            }
            catch (Exception e)
            {
                OnLoadError(e, identifier);
            }

            if (sceneInstance != null)
            {
                appendSceneInstances.Add(sceneInstance);

                if (setArgumentCallback != null)
                {
                    await setArgumentCallback.Invoke(sceneInstance);
                }

                diagnostics.Finish(TimeDiagnostics.Measure.Append);

                var additiveTime = diagnostics.GetTime(TimeDiagnostics.Measure.Append);

                var message = $"{identifier.Value} ({additiveTime:F2}ms)(Additive)";

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

                if (activeOnLoad)
                {
                    sceneInstance.Enable();
                }
            }

            return sceneInstance;
        }

        /// <summary> 加算シーンをアンロード </summary>
        public void UnloadAppendScene(ISceneBase<TScenes> scene, bool deactivateSceneObjects = true)
        {
            var sceneInstance = appendSceneInstances.FirstOrDefault(x => x.Instance == scene);

            if (sceneInstance == null) { return; }

            UnloadAppendScene(sceneInstance, deactivateSceneObjects);
        }

        /// <summary> 加算シーンをアンロード </summary>
        public void UnloadAppendScene(SceneInstance<TScenes> sceneInstance, bool deactivateSceneObjects = true)
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

        /// <summary> 加算シーン遷移 </summary>
        public void AppendTransition<TArgument>(TArgument sceneArgument) where TArgument : ISceneArgument<TScenes>
        {
            // 遷移中は遷移不可.
            if (IsTransition) { return; }

            IsTransition = true;
            
            ObservableEx.FromUniTask(cancelToken => AppendTransitionCore(sceneArgument, cancelToken))
                .Subscribe(_ => IsTransition = false)
                .AddTo(transitionCancelSource.Token);
        }

        /// <summary> 強制加算シーン遷移 </summary>
        public void ForceAppendTransition<TArgument>(TArgument sceneArgument) where TArgument : ISceneArgument<TScenes>
        {
            TransitionCancel();

            IsTransition = true;
            
            ObservableEx.FromUniTask(cancelToken => AppendTransitionCore(sceneArgument, cancelToken))
                .Subscribe(_ => IsTransition = false)
                .AddTo(transitionCancelSource.Token);
        }

        private async UniTask AppendTransitionCore<TArgument>(TArgument sceneArgument, CancellationToken cancelToken) where TArgument : ISceneArgument<TScenes>
        {
            try
            {
                // ロード済みシーンからの遷移制御.

                var handleTransition = await HandleTransitionFromLoadedScenes();

                if (!handleTransition){ return; }

                // 遷移開始.

                var diagnostics = new TimeDiagnostics();

                diagnostics.Begin(TimeDiagnostics.Measure.Total);

                TransitionTarget = sceneArgument.Identifier;

                await TransitionStart(sceneArgument, false);

                if (cancelToken.IsCancellationRequested) { return; }

                // 遷移先以外のシーンを非アクティブ化.

                var enableScenes = loadedScenes.Values.Where(x => x.IsEnable).ToArray();

                foreach (var scene in enableScenes)
                {
                    scene.Disable();
                }

                if (cancelToken.IsCancellationRequested){ return; }

                //====== Append Scene ======

                diagnostics.Begin(TimeDiagnostics.Measure.Load);

                var sceneInstance = await Append(sceneArgument).ToUniTask(cancellationToken:cancelToken);

                diagnostics.Finish(TimeDiagnostics.Measure.Load);

                if (cancelToken.IsCancellationRequested){ return; }

                //====== Scene Prepare ======

                diagnostics.Begin(TimeDiagnostics.Measure.Prepare);

                if (sceneInstance != null)
                {
                    // Prepare通知.
                    if (onPrepare != null)
                    {
                        onPrepare.OnNext(sceneInstance);
                    }

                    await sceneInstance.Instance.Prepare();

                    // Prepare終了通知.
                    if (onPrepareComplete != null)
                    {
                        onPrepareComplete.OnNext(sceneInstance);
                    }
                }

                if (cancelToken.IsCancellationRequested){ return; }

                await TransitionFinish(sceneArgument, false);

                if (cancelToken.IsCancellationRequested){ return; }

                if (sceneInstance != null)
                {
                    //====== Scene Enter ======

                    // Enter通知.
                    if (onEnter != null)
                    {
                        onEnter.OnNext(sceneInstance);
                    }

                    sceneInstance.Instance.Enter();

                    // Enter終了通知.
                    if (onEnterComplete != null)
                    {
                        onEnterComplete.OnNext(sceneInstance);
                    }
                }

                //====== Report ======

                diagnostics.Finish(TimeDiagnostics.Measure.Total);

                var total = diagnostics.GetTime(TimeDiagnostics.Measure.Total);
                var detail = diagnostics.BuildDetailText();

                var message = string.Format("Append Transition: {0} ({1:F2}ms)\n\n{2}", sceneArgument.Identifier, total, detail);

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                TransitionTarget = null;
            }
        }

        /// <summary> 加算シーンアンロード遷移 </summary>
        public void UnloadTransition(TScenes transitionScene, SceneInstance<TScenes> unloadSceneInstance)
        {
            // 遷移中は遷移不可.
            if (IsTransition) { return; }

            IsTransition = true;
            
            ObservableEx.FromUniTask(cancelToken => UnloadTransitionCore(transitionScene, unloadSceneInstance, cancelToken))
                .Subscribe(_ => IsTransition = false)
                .AddTo(transitionCancelSource.Token);
        }

        /// <summary> 加算シーンアンロード遷移 </summary>
        public void UnloadTransition(TScenes transitionScene, GameObject gameObject)
        {
            var sceneInstance = AppendSceneInstances.FirstOrDefault(x => x.GetScene() == gameObject.scene);

            UnloadTransition(transitionScene, sceneInstance);
        }

        /// <summary> 強制加算シーンアンロード遷移 </summary>
        public void ForceUnloadTransition(TScenes transitionScene, SceneInstance<TScenes> unloadSceneInstance)
        {
            TransitionCancel();

            IsTransition = true;

            ObservableEx.FromUniTask(cancelToken => UnloadTransitionCore(transitionScene, unloadSceneInstance, cancelToken))
                .Subscribe(_ => IsTransition = false)
                .AddTo(transitionCancelSource.Token);
        }

        private async UniTask UnloadTransitionCore(TScenes transitionScene, SceneInstance<TScenes> sceneInstance, CancellationToken cancelToken)
        {
            if (sceneInstance == null){ return; }

            try
            {
                var scene = loadedScenes.GetValueOrDefault(transitionScene);

                // ロード済みシーンからの遷移制御.

                var handleTransition = await HandleTransitionFromLoadedScenes();

                if (!handleTransition){ return; }

                if (cancelToken.IsCancellationRequested){ return; }

                // 遷移開始.

                var diagnostics = new TimeDiagnostics();

                diagnostics.Begin(TimeDiagnostics.Measure.Total);

                ISceneArgument<TScenes> argument = null;

                if (scene != null && scene.Instance != null)
                {
                    argument = scene.Instance.GetArgument();
                }

                await TransitionStart(argument, false);

                if (cancelToken.IsCancellationRequested){ return; }

                //====== Scene Leave ======

                diagnostics.Begin(TimeDiagnostics.Measure.Leave);

                // Leave通知.
                if (onLeave != null)
                {
                    onLeave.OnNext(sceneInstance);
                }
                
                await sceneInstance.Instance.Leave();

                // PlayerPrefsを保存.
                PlayerPrefs.Save();

                // Leave終了通知.
                if (onLeaveComplete != null)
                {
                    onLeaveComplete.OnNext(sceneInstance);
                }

                diagnostics.Finish(TimeDiagnostics.Measure.Leave);

                sceneInstance.Disable();
                
                UnloadAppendScene(sceneInstance);

                if (cancelToken.IsCancellationRequested){ return; }

                //====== Scene Active ======

                if (scene == null)
                {
                    Debug.LogError($"UnloadTransition target scene not found.\n{transitionScene}");
                }

                TransitionTarget = scene.Identifier;

                scene.Enable();

                await scene.Instance.OnTransition();

                if (cancelToken.IsCancellationRequested){ return; }

                await TransitionFinish<ISceneArgument<TScenes>>(null, false);

                if (cancelToken.IsCancellationRequested){ return; }

                scene.Instance.Enter();

                //====== Report ======

                diagnostics.Finish(TimeDiagnostics.Measure.Total);

                var total = diagnostics.GetTime(TimeDiagnostics.Measure.Total);
                var detail = diagnostics.BuildDetailText();

                var message = $"Unload Transition: {sceneInstance.Identifier} ({total:F2}ms)\n\n{detail}";

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                TransitionTarget = null;
            }
        }

        /// <summary> 加算シーンインスタンスを検索. </summary>
        public SceneInstance<TScenes> FindAppendSceneInstance(GameObject target)
        {
            if (UnityUtility.IsNull(target)){ return null; }

            return AppendSceneInstances.FirstOrDefault(x => x.GetScene() == target.scene);
        }
    }
}