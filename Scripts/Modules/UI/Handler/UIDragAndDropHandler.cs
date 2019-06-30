﻿
using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Extensions;

namespace Modules.UI
{
    public sealed class UIDragAndDropHandler : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private Subject<Vector2> onDragStart = null;
        private Subject<Vector2> onDrag = null;
        private Subject<Vector2> onDragEnd = null;

        //----- property -----

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
                    if (onDragStart != null)
                    {
                        onDragStart.OnNext(position);
                    }
                })
                .AddTo(this);

            eventTrigger
                .OnDragAsObservable()
                .TakeUntilDisable(this)
                .Where(eventData => eventData.pointerDrag.gameObject == gameObject)
                .Select(eventData => eventData.position)
                .Subscribe(position =>
                {
                    if (onDrag != null)
                    {
                        onDrag.OnNext(position);
                    }
                })
                .AddTo(this);

            eventTrigger
                .OnEndDragAsObservable()
                .TakeUntilDisable(this)
                .Where(eventData => eventData.pointerDrag.gameObject == gameObject)
                .Select(eventData => eventData.position)
                .Subscribe(position =>
                {
                    if (onDragEnd != null)
                    {
                        onDragEnd.OnNext(position);
                    }
                })
                .AddTo(this);
        }

        public IObservable<Vector2> OnDragStartAsObservable()
        {
            return onDragStart ?? (onDragStart = new Subject<Vector2>());
        }

        public IObservable<Vector2> OnDragAsObservable()
        {
            return onDrag ?? (onDrag = new Subject<Vector2>());
        }

        public IObservable<Vector2> OnDragEndAsObservable()
        {
            return onDragEnd ?? (onDragEnd = new Subject<Vector2>());
        }
    }
}
