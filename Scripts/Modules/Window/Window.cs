﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Window
{
    public abstract class Window : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private bool manualDelete = false;

        private Subject<Unit> onOpen = null;
        private Subject<Unit> onClose = null;

        //----- property -----

        public bool ManualDelete
        {
            get { return manualDelete; }
            set { manualDelete = value; }
        }

        //----- method -----

        public IObservable<Unit> OnOpenAsObservable()
        {
            return onOpen ?? (onOpen = new Subject<Unit>());
        }

        public IObservable<Unit> OnCloseAsObservable()
        {
            return onClose ?? (onClose = new Subject<Unit>());
        }

        public IObservable<Unit> Open(bool inputProtect = true)
        {
            var protect = inputProtect ? new InputProtection.InputProtect.Entity() : null;

            return Prepare()
                .Do(_ => UnityUtility.SetActive(gameObject, true))
                .SelectMany(_ => OnOpen())
                .Do(_ =>
                    {
                        if (protect != null)
                        {
                            protect.Dispose();
                            protect = null;
                        }

                        if (onOpen != null)
                        {
                            onOpen.OnNext(Unit.Default);
                        }
                    })
                .AsUnitObservable();
        }

        public IObservable<Unit> Close(bool inputProtect = true)
        {
            var protect = inputProtect ? new InputProtection.InputProtect.Entity() : null;

            return OnClose()
                .Do(_ =>
                    {
                        UnityUtility.SetActive(gameObject, false);

                        if (protect != null)
                        {
                            protect.Dispose();
                            protect = null;
                        }

                        if (onClose != null)
                        {
                            onClose.OnNext(Unit.Default);
                        }
                        
                        if (!manualDelete)
                        {
                            UnityUtility.SafeDelete(gameObject);
                        }
                    })
                .AsUnitObservable();
        }

        public IObservable<Unit> Wait()
        {
            return Observable.FromCoroutine(() => WaitInternal());
        }

        private IEnumerator WaitInternal()
        {
            while (UnityUtility.IsActive(gameObject))
            {
                yield return null;
            }
        }

        protected virtual IObservable<Unit> Prepare() { return Observable.ReturnUnit(); }
        protected virtual IObservable<Unit> OnOpen() { return Observable.ReturnUnit(); }
        protected virtual IObservable<Unit> OnClose() { return Observable.ReturnUnit(); }
    }
}