
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UniRx;
using UniRx.Triggers;
using Extensions;

namespace Modules.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonEventTrigger : MonoBehaviour
    {
        //----- params -----

        private const float DefaultLongPressDuration = 0.5f;

        //----- field -----

        private Button button = null;

        private float? lastPress = null;
        private bool isPress = false;
        private bool isLongPress = false;

        // 押下時の時間.
        private float? pressTime = null;

        // 長押し検知までの時間.
        private float longPressDuration = DefaultLongPressDuration;

        private Subject<Unit> onPress = null;
        private Subject<float> onRelease = null;
        private Subject<Unit> onCancel = null;
        private Subject<Unit> onLongPress = null;
        private Subject<float> onLongPressRelease = null;

        //----- property -----

        public bool HasObservers { get; private set; }

        //----- method -----

        void Awake()
        {
            button = UnityUtility.GetComponent<Button>(gameObject);

            // 押下イベント.
            button.OnPointerDownAsObservable().Subscribe(x => OnPointerDown(x)).AddTo(this);

            // 解放イベント.
            button.OnPointerUpAsObservable().Subscribe(x => OnPointerUp(x)).AddTo(this);

            // 毎フレーム呼び出される.
            button.UpdateAsObservable().Subscribe(_ => OnUpdate()).AddTo(this);

            // 領域外に出たらキャンセル.
            button.OnPointerExitAsObservable().Subscribe(_ => PressCancel()).AddTo(this);

            UpdateObservers();
        }

        private void UpdateObservers()
        {
            var hasObservers = false;

            hasObservers |= onPress != null && onPress.HasObservers;
            hasObservers |= onRelease != null && onRelease.HasObservers;
            hasObservers |= onCancel != null && onCancel.HasObservers;
            hasObservers |= onLongPress != null && onLongPress.HasObservers;
            hasObservers |= onLongPressRelease != null && onLongPressRelease.HasObservers;

            HasObservers = hasObservers;
        }

        private void OnPointerDown(PointerEventData pointerEvent)
        {
            if (0 < pointerEvent.pointerId) { return; }

            if (!button.interactable) { return; }

            isPress = true;
            lastPress = Time.realtimeSinceStartup;

            pressTime = lastPress;

            if (onPress != null)
            {
                onPress.OnNext(Unit.Default);
            }
        }

        private void OnPointerUp(PointerEventData pointerEvent)
        {
            if (0 < pointerEvent.pointerId) { return; }

            if (!button.interactable) { return; }

            var time = pressTime.HasValue ? Time.realtimeSinceStartup - pressTime.Value : 0f;

            if (isPress)
            {
                isPress = false;
                lastPress = null;

                if (onRelease != null)
                {
                    onRelease.OnNext(time);
                }
            }

            if (isLongPress)
            {
                isLongPress = false;

                if (onLongPressRelease != null)
                {
                    onLongPressRelease.OnNext(time);
                }
            }
        }

        private void OnUpdate()
        {
            // InputProtectionで無効化された際に押下状態を解除.
            if (!button.enabled)
            {
                PressCancel();
            }

            // interactableが無効な場合解除.
            if (!button.interactable)
            {
                PressCancel();
            }

            // 長押しを検知.
            if (onLongPress != null && isPress && lastPress.HasValue)
            {
                var timeNow = Time.realtimeSinceStartup;

                if (lastPress.Value + longPressDuration <= timeNow)
                {
                    isPress = false;
                    isLongPress = true;

                    lastPress = null;

                    onLongPress.OnNext(Unit.Default);
                }
            }
        }

        public void SetLongPressDuration(float duration)
        {
            longPressDuration = duration;
        }

        public IObservable<Unit> OnPressAsObservable()
        {
            if(onPress == null)
            {
                onPress = new Subject<Unit>();

                onPress.ObserveEveryValueChanged(x => x.HasObservers)
                    .Subscribe(_ => UpdateObservers())
                    .AddTo(this);
            }

            return onPress;
        }

        public IObservable<float> OnReleaseAsObservable()
        {
            if(onRelease == null)
            {
                onRelease = new Subject<float>();

                onRelease.ObserveEveryValueChanged(x => x.HasObservers)
                    .Subscribe(_ => UpdateObservers())
                    .AddTo(this);
            }

            return onRelease;
        }

        public IObservable<Unit> OnCancelAsObservable()
        {
            if(onCancel == null)
            {
                onCancel = new Subject<Unit>();

                onCancel.ObserveEveryValueChanged(x => x.HasObservers)
                    .Subscribe(_ => UpdateObservers())
                    .AddTo(this);
            }

            return onCancel;
        }

        public IObservable<Unit> OnLongPressAsObservable()
        {
            OnPressAsObservable().Subscribe().AddTo(this);
            OnReleaseAsObservable().Subscribe().AddTo(this);

            if(onLongPress == null)
            {
                onLongPress = new Subject<Unit>();

                onLongPress.ObserveEveryValueChanged(x => x.HasObservers)
                    .Subscribe(_ => UpdateObservers())
                    .AddTo(this);
            }

            return onLongPress;
        }

        public IObservable<float> OnLongPressReleaseAsObservable()
        {
            if(onLongPressRelease == null)
            {
                onLongPressRelease = new Subject<float>();

                onLongPressRelease.ObserveEveryValueChanged(x => x.HasObservers)
                    .Subscribe(_ => UpdateObservers())
                    .AddTo(this);
            }

            return onLongPressRelease;
        }

        private void PressCancel()
        {
            if (isPress || isLongPress)
            {
                isPress = false;
                isLongPress = false;
                lastPress = null;

                lastPress = null;
                pressTime = null;

                if (onCancel != null)
                {
                    onCancel.OnNext(Unit.Default);
                }
            }
        }
    }
}