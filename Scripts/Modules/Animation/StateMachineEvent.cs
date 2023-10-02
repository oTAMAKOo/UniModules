
namespace Modules.Animation
{
    public enum StateMachineEventType
    {
        EnterState,
        ExitState,
    }

    public sealed class StateMachineEvent
    {
        public string ParameterName { get; private set; }
        public StateMachineEventType EventType { get; private set; }

        public StateMachineEvent(string parameterName, StateMachineEventType eventType)
        {
            ParameterName = parameterName;
            EventType = eventType;
        }
    }
}
