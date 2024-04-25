
using UnityEngine;
using System;
using UniRx;

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

        public IObservable<StateMachineEvent> OnStateMachineEventAsObservable()
        {
            return onStateMachineEvent ?? (onStateMachineEvent = new Subject<StateMachineEvent>());
        }
    }
}