
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Modules.UI
{
    public sealed class FadeGraphic
    {
        //----- params -----

        //----- field -----

		private Func<float> getAlpha = null;

		private bool ignoreTimeScale = false;

		private float duration = 0f;

		private float fadeAlpha = 0f;

		private Subject<float> onChangeAlpha = null;
		private Subject<bool> onChangeActive = null;

        //----- property -----

        //----- method -----

		public FadeGraphic(float duration, Func<float> getAlpha, bool ignoreTimeScale = false)
		{
			this.duration = duration;
			this.getAlpha = getAlpha;
			this.ignoreTimeScale = ignoreTimeScale;

			if (getAlpha == null)
			{
				throw new ArgumentException("Callback getAlpha is null");
			}

			fadeAlpha = getAlpha.Invoke();
		}
		
		public async UniTask FadeOut()
		{
			if (onChangeActive != null)
			{
				onChangeActive.OnNext(true);
			}

			fadeAlpha = getAlpha.Invoke();

			while (fadeAlpha < 1f)
			{
				var deltaTime = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

				fadeAlpha += deltaTime / duration;

				SetFadeAlpha(fadeAlpha);

				await UniTask.NextFrame();
			}

			fadeAlpha = 1f;

			SetFadeAlpha(fadeAlpha);
		}

		public async UniTask FadeIn()
		{
			if (onChangeActive != null)
			{
				onChangeActive.OnNext(true);
			}

			fadeAlpha = getAlpha.Invoke();

			while (0 < fadeAlpha)
			{
				var deltaTime = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

				fadeAlpha -= deltaTime / duration;
               
				SetFadeAlpha(fadeAlpha);

				await UniTask.NextFrame();
			}

			fadeAlpha = 0f;

			SetFadeAlpha(fadeAlpha);
		}

		private void SetFadeAlpha(float value)
		{
			fadeAlpha = Mathf.Clamp(value, 0f, 1f);

			if (onChangeAlpha != null)
			{
				onChangeAlpha.OnNext(fadeAlpha);
			}

			if (onChangeActive != null)
			{
				onChangeActive.OnNext(0f < fadeAlpha);
			}
		}

		public IObservable<float> OnChangeAlphaAsObservable()
		{
			return onChangeAlpha ?? (onChangeAlpha = new Subject<float>());
		}
		
		public IObservable<bool> OnChangeActiveAsObservable()
		{
			return onChangeActive ?? (onChangeActive = new Subject<bool>());
		}
    }
}