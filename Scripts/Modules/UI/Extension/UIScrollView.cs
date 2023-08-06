﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(ScrollRect))]
    public abstract class UIScrollView : UIComponent<ScrollRect>
    {
        //----- params -----

        //----- field -----

        private Vector2? position = null;
        private IDisposable lockPositionDisposable = null;

        //----- property -----

        public ScrollRect ScrollRect { get { return component; } }

        public GameObject ContentRoot
        {
            get
            {
                if (component == null){ return null; }

                if (component.content == null){ return null; }

                return component.content.gameObject;
            }
        }

        //----- method -----

        public void LockPosition()
        {
            if (position.HasValue) { return; }

            position = ScrollRect.normalizedPosition;

            lockPositionDisposable = Observable.EveryUpdate()
                .Subscribe(_ =>
                    {
                        if (!UnityUtility.IsNull(ScrollRect))
                        {
                            ScrollRect.normalizedPosition = position.Value;
                        }
                    })
                .AddTo(this);
        }

        public void UnLockPosition()
        {
            if (position.HasValue)
            {
                ScrollRect.normalizedPosition = position.Value;
                position = null;
            }

            if (lockPositionDisposable != null)
            {
                lockPositionDisposable.Dispose();
                lockPositionDisposable = null;
            }
        }

        public void ResetPosition()
        {
            if (ScrollRect.horizontal)
            {
                ScrollRect.horizontalNormalizedPosition = 0f;
            }

            if (ScrollRect.vertical)
            {
                ScrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}
