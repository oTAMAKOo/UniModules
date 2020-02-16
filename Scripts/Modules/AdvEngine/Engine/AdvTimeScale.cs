
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.AdvKit
{
    public class AdvTimeScale : LifetimeDisposable
    {
        //----- params -----

        public const float DefaultTimeScale = 1f;

        //----- field -----

        private float? changeTimeScale = null;

        private Subject<float> onTimeScaleChanged = null;

        private bool initialized = false;

        //----- property -----

        public float Current { get; private set; }

        //----- method -----

        public virtual void Initialize()
        {
            if (initialized) { return; }

            Current = DefaultTimeScale;

            Observable.EveryUpdate()
                .Subscribe(_ => UpdateTimeScale())
                .AddTo(Disposable);

            initialized = true;
        }

        private void UpdateTimeScale()
        {
            var timeScale = DefaultTimeScale;

            var customTimeScale = GetCustomTimeScale();

            timeScale = Mathf.Max(timeScale, customTimeScale);

            if (changeTimeScale.HasValue)
            {
                timeScale = Mathf.Max(timeScale, changeTimeScale.Value);
            }

            if (Current != timeScale)
            {
                Current = timeScale;

                if (onTimeScaleChanged != null)
                {
                    onTimeScaleChanged.OnNext(Current);
                }
            }
        }

        public void ChangeTimeScale(float timeScale)
        {
            changeTimeScale = timeScale;
        }

        public void ResetTimeScale()
        {
            changeTimeScale = DefaultTimeScale;
        }

        protected virtual float GetCustomTimeScale()
        {
            return 0f;
        }

        public IObservable<float> OnTimeScaleChangedAsObservable()
        {
            return onTimeScaleChanged ?? (onTimeScaleChanged = new Subject<float>());
        }
    }
}
