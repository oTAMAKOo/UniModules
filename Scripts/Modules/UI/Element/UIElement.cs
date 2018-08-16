﻿﻿
using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    public abstract class UIElement<T> : MonoBehaviour where T : Component
    {
        //----- params -----

        //----- field -----

        private T targetComponent = null;

        private bool waitRecovery = false;
        private bool? recoveryState = null;
        private IDisposable recoveryDisposable = null;

        //----- property -----

        public T component
        {
            get
            {
                if (targetComponent == null)
                {
                    targetComponent = UnityUtility.GetComponent<T>(gameObject);
                }

                return targetComponent;
            }
        }

        //----- method -----

        protected virtual void OnEnable()
        {
            Modify();

            Recovery();
        }

        protected virtual void OnDisable()
        {
            WaitRecovery();
        }

        private void OnRectTransformParentChanged()
        {
            Modify();
        }

        public abstract void Modify();

        #region Recovery

        private void Recovery()
        {
            if (!Application.isPlaying) { return; }

            if (!waitRecovery) { return; }

            var target = component as Selectable;

            if (recoveryDisposable != null)
            {
                if (recoveryState.HasValue)
                {
                    target.enabled = recoveryState.Value;
                    recoveryState = null;
                }

                recoveryDisposable.Dispose();
                recoveryDisposable = null;
            }

            if (target != null)
            {
                recoveryState = target.enabled;

                target.enabled = !recoveryState.Value;

                recoveryDisposable = Observable.EveryUpdate()
                    .SkipWhile(_ => !UnityUtility.IsActiveInHierarchy(target.gameObject))
                    .Take(1)
                    .Subscribe(_ => target.enabled = recoveryState.Value)
                    .AddTo(this);

                waitRecovery = false;
            }
        }

        private void WaitRecovery()
        {
            if (!Application.isPlaying) { return; }

            var target = component as Selectable;

            if (recoveryDisposable != null)
            {
                if (recoveryState.HasValue)
                {
                    target.enabled = recoveryState.Value;
                    recoveryState = null;
                }

                recoveryDisposable.Dispose();
                recoveryDisposable = null;
            }

            waitRecovery = true;
        }

        #endregion
    }
}