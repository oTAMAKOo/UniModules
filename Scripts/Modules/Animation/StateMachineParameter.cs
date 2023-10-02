﻿
using UnityEngine;

namespace Modules.Animation
{
    public abstract class StateMachineParameter
    {
        public string ParameterName { get; private set; }

        protected StateMachineParameter(string parameterName)
        {
            this.ParameterName = parameterName;
        }

        public abstract void SetParameter(Animator target);
    }

    public sealed class IntStateMachineParameter : StateMachineParameter
    {
        public int Value { get; private set; }

        public IntStateMachineParameter(string parameterName, int value) : base(parameterName)
        {
            this.Value = value;
        }

        public override void SetParameter(Animator target)
        {
            target.SetInteger(this.ParameterName, this.Value);
        }
    }

    public sealed class FloatStateMachineParameter : StateMachineParameter
    {
        public float Value { get; private set; }

        public FloatStateMachineParameter(string parameterName, float value) : base(parameterName)
        {
            this.Value = value;
        }

        public override void SetParameter(Animator target)
        {
            target.SetFloat(this.ParameterName, this.Value);
        }
    }

    public sealed class BoolStateMachineParameter : StateMachineParameter
    {
        public bool Value { get; private set; }

        public BoolStateMachineParameter(string parameterName, bool value) : base(parameterName)
        {
            this.Value = value;
        }

        public override void SetParameter(Animator target)
        {
            target.SetBool(this.ParameterName, this.Value);
        }
    }

    public sealed class TriggerStateMachineParameter : StateMachineParameter
    {
        public TriggerStateMachineParameter(string parameterName) : base(parameterName) { }

        public override void SetParameter(Animator target)
        {
            target.SetTrigger(this.ParameterName);
        }
    }

    public sealed class TriggerResetStateMachineParameter : StateMachineParameter
    {
        public TriggerResetStateMachineParameter(string parameterName) : base(parameterName) { }

        public override void SetParameter(Animator target)
        {
            target.ResetTrigger(this.ParameterName);
        }
    }
}
