
using UnityEngine;
using System;
using UniRx;
using UniRx.Triggers;
using Extensions;

namespace Modules.UI
{
    public sealed class UISwipeHandler : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private float thresholdSenconds = 1.0f;
        [SerializeField]
        private float thresholdDistance = 100.0f;

        private Vector2 beginPosition;
        private DateTime beginTime;

        private Subject<Unit> onSwipeLeft = null;
        private Subject<Unit> onSwipeRight = null;
        private Subject<Unit> onSwipeDown = null;
        private Subject<Unit> onSwipeUp = null;

        //----- property -----

        public bool IsSwipe { get; private set; }

        //----- method -----

        void OnEnable()
        {
            var eventTrigger = UnityUtility.GetOrAddComponent<ObservableEventTrigger>(gameObject);

            eventTrigger
                .OnBeginDragAsObservable()
                .TakeUntilDisable(this)
                .Where(eventData => eventData.pointerDrag.gameObject == gameObject)
                .Select(eventData => eventData.position)
                .Subscribe(position =>
                {
                    IsSwipe = true;
                    beginPosition = position;
                    beginTime = DateTime.Now;
                })
                .AddTo(this);

            var onEndDragObservable = eventTrigger
                .OnEndDragAsObservable()
                .TakeUntilDisable(this)
                .Where(eventData => (DateTime.Now - beginTime).TotalSeconds < thresholdSenconds)
                .Select(eventData => eventData.position)
                .Share();

            // left.
            onEndDragObservable
                .Where(position => beginPosition.x > position.x)
                .Where(position => Mathf.Abs(beginPosition.x - position.x) >= thresholdDistance)
                .Subscribe(_ =>
                {
                    if (onSwipeLeft != null)
                    {
                        onSwipeLeft.OnNext(Unit.Default);
                    }
                })
                .AddTo(this);

            // right.
            onEndDragObservable
                .Where(position => position.x > beginPosition.x)
                .Where(position => Mathf.Abs(position.x - beginPosition.x) >= thresholdDistance)
                .Subscribe(_ =>
                {
                    if (onSwipeRight != null)
                    {
                        onSwipeRight.OnNext(Unit.Default);
                    }
                })
                .AddTo(this);

            // down.
            onEndDragObservable
                .Where(position => beginPosition.y > position.y)
                .Where(position => Mathf.Abs(beginPosition.y - position.y) >= thresholdDistance)
                .Subscribe(_ =>
                {
                    if (onSwipeDown != null)
                    {
                        onSwipeDown.OnNext(Unit.Default);
                    }
                })
                .AddTo(this);

            // up.
            onEndDragObservable
                .Where(position => position.y > beginPosition.y)
                .Where(position => Mathf.Abs(position.y - beginPosition.y) >= thresholdDistance)
                .Subscribe(_ =>
                {
                    if (onSwipeUp != null)
                    {
                        onSwipeUp.OnNext(Unit.Default);
                    }
                })
                .AddTo(this);

            onEndDragObservable.Subscribe(_ => IsSwipe = false).AddTo(this);
        }

        public IObservable<Unit> OnSwipeLeftAsObservable()
        {
            return onSwipeLeft ?? (onSwipeLeft = new Subject<Unit>());
        }

        public IObservable<Unit> OnSwipeRightAsObservable()
        {
            return onSwipeRight ?? (onSwipeRight = new Subject<Unit>());
        }

        public IObservable<Unit> OnSwipeDownAsObservable()
        {
            return onSwipeDown ?? (onSwipeDown = new Subject<Unit>());
        }

        public IObservable<Unit> OnSwipeUpAsObservable()
        {
            return onSwipeUp ?? (onSwipeUp = new Subject<Unit>());
        }
    }
}
