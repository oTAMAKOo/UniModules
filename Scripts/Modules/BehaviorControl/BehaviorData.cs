
using System;
using System.Collections.Generic;

namespace Modules.BehaviorControl
{
    public enum ConditionConnecter
    {
        None = 0,

        And,
        Or,
    }

    public abstract class BehaviorData<TAction, TTarget, TCondition> where TAction : Enum where TTarget : Enum where TCondition : Enum
    {
        [Serializable]
        public sealed class Behavior
        {
            /// <summary>  </summary>
            public float SuccessRate { get; set; }

            /// <summary>  </summary>
            public TAction ActionType { get; set; }

            /// <summary>  </summary>
            public string ActionParameters { get; set; }

            /// <summary>  </summary>
            public TTarget TargetType { get; set; }

            /// <summary>  </summary>
            public string TargetParameters { get; set; }

            /// <summary>  </summary>
            public Condition[] Conditions { get; set; }
        }

        [Serializable]
        public sealed class Condition
        {
            /// <summary>  </summary>
            public TCondition Type { get; set; }

            /// <summary>  </summary>
            public string Parameters { get; set; }

            /// <summary>  </summary>
            public ConditionConnecter Connecter { get; set; }
        }

        public string Description { get; set; }

        public IReadOnlyList<Behavior> Behaviors { get; set; }
    }
}
