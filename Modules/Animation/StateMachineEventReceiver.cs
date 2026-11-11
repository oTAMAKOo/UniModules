
using UnityEngine;
using System;
using R3;

namespace Modules.Animation
{
    [RequireComponent(typeof(Animator))]
    public class StateMachineEventReceiver : MonoBehaviour, IStateMachineEventHandler
    {
        //----- params -----

        //----- field -----

        // StateMachineイベント.
        private Subject<StateMachineEvent> onStateMachineEvent = null;

        //----- property -----

        //----- method -----

        public void StateMachineEvent(StateMachineEvent stateMachineEvent)
        {
            if (onStateMachineEvent != null)
            {
                onStateMachineEvent.OnNext(stateMachineEvent);
            }
        }

        public Observable<StateMachineEvent> OnStateMachineEventAsObservable()
        {
            return onStateMachineEvent ?? (onStateMachineEvent = new Subject<StateMachineEvent>());
        }
    }
}