
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.Window
{
    public sealed class TouchBlock : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private CanvasGroup canvasGroup = null;
        [SerializeField]
        private Button blockTouch = null;
        [SerializeField]
        private float fadeAmount = 0.1f;

        private float fadeAlpha = 0f;

        private Subject<Unit> onBlockTouch = null;

        private bool initialized = false;

        //----- property -----

        public bool Active { get; private set; }

        //----- method -----

        public void Initialize()
        {
            if (initialized) { return; }

            if (blockTouch != null)
            {
                Action onTouchBloc = () =>
                {
                    if (onBlockTouch != null)
                    {
                        onBlockTouch.OnNext(Unit.Default);
                    }
                };

                blockTouch.OnClickAsObservable()
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

        public async UniTask FadeOut(CancellationToken cancelToken = default)
        {
            UnityUtility.SetActive(canvasGroup.gameObject, true);

            try
            {
                while (0 < fadeAlpha)
                {
                    fadeAlpha -= fadeAmount;

                    SetAlpha(fadeAlpha);

                    await UniTask.NextFrame(cancelToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            
            SetAlpha(0f);
        }

        public async UniTask FadeIn(CancellationToken cancelToken = default)
        {
            UnityUtility.SetActive(canvasGroup.gameObject, true);

            try
            {
                while (fadeAlpha < 1)
                {
                    fadeAlpha += fadeAmount;

                    SetAlpha(fadeAlpha);

                    await UniTask.NextFrame(cancelToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            
            SetAlpha(1f);
        }

        public IObservable<Unit> OnBlockTouchAsObservable()
        {
            return onBlockTouch ?? (onBlockTouch = new Subject<Unit>());
        }
    }
}
