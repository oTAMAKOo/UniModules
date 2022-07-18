
using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using Extensions;
using Modules.TimeUtil;

namespace Modules.Tweening
{
	public sealed class TweenController : LifetimeDisposable
	{
		//----- params -----

		//----- field -----

		private List<Tweener> tweeners = null;
		
		private TimeScale timeScale = null;

		//----- property -----

		public float TimeScale
		{
			get { return timeScale.Value; }
			set { timeScale.Value = value; }
		}

		//----- method -----

		public TweenController()
		{
			timeScale = new TimeScale();

			tweeners = new List<Tweener>();

			timeScale.OnTimeScaleChangedAsObservable()
				.Subscribe(x => OnTimeScaleUpdate(x))
				.AddTo(Disposable);
		}

		public async UniTask Play(Tweener tweener)
		{
			tweeners.Add(tweener);

			try
			{
				var _ = tweener.Pause();

				tweener.timeScale = timeScale.Value;

				await tweener.Play().ToUniTask();
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			tweeners.Remove(tweener);
		}

		private void OnTimeScaleUpdate(float value)
		{
			foreach (var tweener in tweeners)
			{
				tweener.timeScale = value;
			}
		}
	}
}
