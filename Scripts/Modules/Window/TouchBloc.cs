
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UniRx;
using Extensions;

namespace Modules.Window
{
    public sealed class TouchBloc : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private CanvasGroup canvasGroup = null;
        [SerializeField]
        private Button blocTouch = null;
        [SerializeField]
        private float fadeAmount = 0.1f;

        private float fadeAlpha = 0f;

        private Subject<Unit> onBlocTouch = null;

        private bool initialized = false;

        //----- property -----

        public bool Active { get; private set; }

        //----- method -----

        public void Initialize()
        {
            if (initialized) { return; }

            if (blocTouch != null)
            {
                Action onTouchBloc = () =>
                {
                    if (onBlocTouch != null)
                    {
                        onBlocTouch.OnNext(Unit.Default);
                    }
                };

                blocTouch.OnClickAsObservable()
                    .Subscribe(_ => onTouchBloc.Invoke())
                    .AddTo(this);
            }

            SetAlpha(0f);

            initialized = true;
        }

        public void SetAlpha(float value)
        {
            fadeAlpha = Mathf.Clamp(value, 0f, 1f);
            canvasGroup.alpha = fadeAlpha;

            Active = 0 < fadeAlpha;

            UnityUtility.SetActive(canvasGroup.gameObject, Active);
        }

        public void Hide()
        {
            SetAlpha(0f);
        }

        public IObservable<Unit> FadeIn()
        {
            return Observable.FromMicroCoroutine(() => FadeInAsync());
        }

        public IObservable<Unit> FadeOut()
        {
            return Observable.FromMicroCoroutine(() => FadeOutAsync());
        }

        private IEnumerator FadeOutAsync()
        {
            UnityUtility.SetActive(canvasGroup.gameObject, true);

            while (0 < fadeAlpha)
            {
                fadeAlpha -= fadeAmount;

                SetAlpha(fadeAlpha);

                yield return null;
            }
            
            SetAlpha(0f);
        }

        private IEnumerator FadeInAsync()
        {
            UnityUtility.SetActive(canvasGroup.gameObject, true);

            while (fadeAlpha < 1)
            {
                fadeAlpha += fadeAmount;

                SetAlpha(fadeAlpha);

                yield return null;
            }
            
            SetAlpha(1f);
        }

        public IObservable<Unit> OnBlocTouchAsObservable()
        {
            return onBlocTouch ?? (onBlocTouch = new Subject<Unit>());
        }
    }
}
