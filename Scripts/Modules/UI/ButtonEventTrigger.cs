
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using Extensions;
using UnityEngine.EventSystems;

namespace Modules.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonEventTrigger : MonoBehaviour
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
            return onPress ?? (onPress = new Subject<Unit>());
        }

        public IObservable<float> OnReleaseAsObservable()
        {
            return onRelease ?? (onRelease = new Subject<float>());
        }

        public IObservable<Unit> OnCancelAsObservable()
        {
            return onCancel ?? (onCancel = new Subject<Unit>());
        }

        public IObservable<Unit> OnLongPressAsObservable()
        {
            OnPressAsObservable().Subscribe().AddTo(this);
            OnReleaseAsObservable().Subscribe().AddTo(this);

            return onLongPress ?? (onLongPress = new Subject<Unit>());
        }

        public IObservable<float> OnLongPressReleaseAsObservable()
        {
            return onLongPressRelease ?? (onLongPressRelease = new Subject<float>());
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