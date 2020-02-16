
using UnityEngine;
using System;
using UniRx;
using Extensions;

namespace Modules.AdvKit
{
    [DisallowMultipleComponent]
    public class AdvObject : MonoBehaviour
    {
        #if ENABLE_MOONSHARP

        //----- params -----

        //----- field -----
        
        private Subject<Unit> onChangePriority = null;

        private bool initialized = false;

        //----- property -----

        public string Identifier { get; private set; }

        public int Priority { get; private set; }

        //----- method -----

        public void Initialize(string identifier)
        {
            if (initialized) { return; }

            Identifier = identifier;

            transform.name = identifier;

            UnityUtility.SetActive(gameObject, false);

            OnInitialize();

            SetPriority(Priority);

            initialized = true;
        }

        public void SetPriority(int priority)
        {
            Priority = priority;

            if (onChangePriority != null)
            {
                onChangePriority.OnNext(Unit.Default);
            }
        }

        public IObservable<Unit> OnChangePriorityAsObservable()
        {
            return onChangePriority ?? (onChangePriority = new Subject<Unit>());
        }

        protected virtual void OnInitialize() { }

        #endif
    }
}
