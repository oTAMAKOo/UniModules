
using UnityEngine;
using Unity.Linq;
using System.Linq;

namespace Modules.Animation
{
    public interface IStateMachineEventHandler
    {
        void StateMachineEvent(StateMachineEvent stateMachineEvent);
    }

	public sealed class StateMachineTrigger : StateMachineBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string parameterName = string.Empty;
        [SerializeField]
        private StateMachineEventType eventType = StateMachineEventType.EnterState;

        //----- property -----

        //----- method -----

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (eventType != StateMachineEventType.EnterState) { return; }

            SendStateEvent<StateMachineEventReceiver>(animator);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (eventType != StateMachineEventType.ExitState) { return; }

            SendStateEvent<StateMachineEventReceiver>(animator);
        }

        public void SendStateEvent<T>(Animator animator) where T : Component, IStateMachineEventHandler
        {
            var target = animator.gameObject.AncestorsAndSelf().OfComponent<T>().FirstOrDefault();

            if (target == null)
            {
                Debug.LogWarningFormat("Fail to force end because of {0} is not found.", typeof(T).Name);
                return;
            }

            target.StateMachineEvent(new StateMachineEvent(parameterName, eventType));
        }
    }
}
