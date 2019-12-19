﻿﻿
using UnityEngine;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.StateMachine
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
