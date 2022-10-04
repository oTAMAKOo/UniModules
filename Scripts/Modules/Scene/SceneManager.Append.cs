
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Constants;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;
using Modules.Scene.Diagnostics;

namespace Modules.Scene
{
	public abstract partial class SceneManager<T>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		/// <summary>
        /// シーンを追加で読み込み.
        /// <para> Prepare, Enter, Leaveは自動で呼び出されないので自分で制御する </para>
        /// </summary>
        public IObservable<SceneInstance> Append<TArgument>(TArgument sceneArgument, bool activeOnLoad = true) where TArgument : ISceneArgument
        {
            return ObservableEx.FromUniTask(cancelToken => AppendCore(sceneArgument.Identifier, activeOnLoad, cancelToken))
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
            return ObservableEx.FromUniTask(cancelToken => AppendCore(identifier, activeOnLoad, cancelToken));
        }

        private async UniTask<SceneInstance> AppendCore(Scenes? identifier, bool activeOnLoad, CancellationToken cancelToken)
        {
            if (!identifier.HasValue) { return null; }

            SceneInstance sceneInstance = null;

            var diagnostics = new TimeDiagnostics();

            diagnostics.Begin(TimeDiagnostics.Measure.Append);

			try
			{
				var loadYield = LoadScene(identifier.Value, LoadSceneMode.Additive).ToYieldInstruction(cancelToken);

				while (!loadYield.IsDone)
				{
					await UniTask.NextFrame(cancelToken);
				}

				sceneInstance = loadYield.Result;
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

		/// <summary> 加算シーン遷移 </summary>
		public void AppendTransition<TArgument>(TArgument sceneArgument) where TArgument :ISceneArgument
        {
			// 遷移中は遷移不可.
			if (IsTransition) { return; }

			IsTransition = true;
			
			ObservableEx.FromUniTask(cancelToken => AppendTransitionCore(sceneArgument, cancelToken))
				.Subscribe(_ => IsTransition = false)
				.AddTo(transitionCancelSource.Token);
        }

        private async UniTask AppendTransitionCore<TArgument>(TArgument sceneArgument, CancellationToken cancelToken) where TArgument :ISceneArgument
        {
			var diagnostics = new TimeDiagnostics();

			diagnostics.Begin(TimeDiagnostics.Measure.Total);

			try
			{
				await TransitionStart(sceneArgument).AttachExternalCancellation(cancelToken);

				// 遷移先以外のシーンを非アクティブ化.

				var enableScenes = loadedScenes.Values.Where(x => x.IsEnable).ToArray();

				foreach (var scene in enableScenes)
				{
					scene.Disable();
				}

				//====== Append Scene ======

				diagnostics.Begin(TimeDiagnostics.Measure.Load);

				var sceneInstance = await Append(sceneArgument).ToUniTask(cancellationToken:cancelToken);

				diagnostics.Finish(TimeDiagnostics.Measure.Load);

				//====== Scene Prepare ======

				diagnostics.Begin(TimeDiagnostics.Measure.Prepare);

				if (sceneInstance != null)
				{
					// Prepare通知.
					if (onPrepare != null)
					{
						onPrepare.OnNext(currentSceneArgument);
					}

					await sceneInstance.Instance.Prepare().AttachExternalCancellation(cancelToken);

					// Prepare終了通知.
					if (onPrepareComplete != null)
					{
						onPrepareComplete.OnNext(currentSceneArgument);
					}
				}

				await TransitionFinish(sceneArgument).AttachExternalCancellation(cancelToken);

				if (sceneInstance != null)
				{
					//====== Scene Enter ======

					// Enter通知.
					if (onEnter != null)
					{
						onEnter.OnNext(currentSceneArgument);
					}

					sceneInstance.Instance.Enter();

					// Enter終了通知.
					if (onEnterComplete != null)
					{
						onEnterComplete.OnNext(currentSceneArgument);
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
				return;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
        }

		/// <summary> 加算シーンアンロード遷移 </summary>
        public void UnloadTransition(Scenes transitionScene, SceneInstance unloadSceneInstance)
        {
			// 遷移中は遷移不可.
			if (IsTransition) { return; }

			IsTransition = true;
			
			ObservableEx.FromUniTask(cancelToken => UnloadTransitionCore(transitionScene, unloadSceneInstance, cancelToken))
				.Subscribe(_ => IsTransition = false)
				.AddTo(transitionCancelSource.Token);
        }

		/// <summary> 加算シーンアンロード遷移 </summary>
		public void UnloadTransition(Scenes transitionScene, GameObject gameObject)
		{
			var sceneInstance = AppendSceneInstances.FirstOrDefault(x => x.GetScene() == gameObject.scene);

			UnloadTransition(transitionScene, sceneInstance);
		}

        private async UniTask UnloadTransitionCore(Scenes transitionScene, SceneInstance sceneInstance, CancellationToken cancelToken)
        {
            if (sceneInstance == null){ return; }

			var diagnostics = new TimeDiagnostics();

			diagnostics.Begin(TimeDiagnostics.Measure.Total);

            try
            {
				await TransitionStart<ISceneArgument>(null).AttachExternalCancellation(cancelToken);

				//====== Scene Leave ======

				diagnostics.Begin(TimeDiagnostics.Measure.Leave);

				var prevSceneArgument = sceneInstance.Instance.GetArgument();

				// Leave通知.
				if (onLeave != null)
				{
					onLeave.OnNext(prevSceneArgument);
				}
				
                await sceneInstance.Instance.Leave().AttachExternalCancellation(cancelToken);

				// PlayerPrefsを保存.
				PlayerPrefs.Save();

				// Leave終了通知.
				if (onLeaveComplete != null)
				{
					onLeaveComplete.OnNext(prevSceneArgument);
				}

				diagnostics.Finish(TimeDiagnostics.Measure.Leave);

                sceneInstance.Disable();
                
                UnloadAppendScene(sceneInstance);

				//====== Scene Active ======

				var scene = loadedScenes.GetValueOrDefault(transitionScene);

				scene.Enable();

                await TransitionFinish<ISceneArgument>(null).AttachExternalCancellation(cancelToken);

				//====== Report ======

				diagnostics.Finish(TimeDiagnostics.Measure.Total);

				var total = diagnostics.GetTime(TimeDiagnostics.Measure.Total);
				var detail = diagnostics.BuildDetailText();

				var message = string.Format("Unload Transition: {0} ({1:F2}ms)\n\n{2}", sceneInstance.Identifier, total, detail);

				UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
		}
	}
}