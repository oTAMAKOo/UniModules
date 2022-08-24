
using System;
using UniRx;
using Extensions;

namespace Modules.TimeUtil
{
    public sealed class TimeScale : LifetimeDisposable
    {
        //----- params -----

		public const float DefaultTimeScale = 1f;

		//----- field -----
		
		private float current = DefaultTimeScale;

		private Subject<float> onTimeScaleChanged = null;

		private bool initialized = false;

		//----- property -----
		
		public float Value
		{
			get { return current; }
			
			set
			{
				ChangeTimeScale(value);
			}
		}

		//----- method -----

		public void Initialize()
		{
			if (initialized) { return; }

			Value = DefaultTimeScale;

			initialized = true;
		}

		private void ChangeTimeScale(float timeScale)
		{
			current = timeScale;

			if (onTimeScaleChanged != null)
			{
				onTimeScaleChanged.OnNext(current);
			}
		}

		public void Reset()
		{
			Value = DefaultTimeScale;
		}

		public IObservable<float> OnTimeScaleChangedAsObservable()
		{
			return onTimeScaleChanged ?? (onTimeScaleChanged = new Subject<float>());
		}
    }
}