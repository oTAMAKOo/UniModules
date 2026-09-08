
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
            /// <summary> 実行確率 (0f - 1f) </summary>
            public float SuccessRate { get; set; }

            /// <summary> 行動タイプ </summary>
            public TAction ActionType { get; set; }

            /// <summary> 行動パラメータ </summary>
            public string ActionParameters { get; set; }

            /// <summary> 対象選択タイプ </summary>
            public TTarget TargetType { get; set; }

            /// <summary> 対象選択パラメータ </summary>
            public string TargetParameters { get; set; }

            /// <summary> 条件 </summary>
            public Condition[] Conditions { get; set; }
        }

        [Serializable]
        public sealed class Condition
        {
            /// <summary> 条件タイプ </summary>
            public TCondition Type { get; set; }

            /// <summary> 条件パラメータ </summary>
            public string Parameters { get; set; }

            /// <summary> 条件接続詞 </summary>
            public ConditionConnecter Connecter { get; set; }
        }

        /// <summary> 説明 </summary>
        public string Description { get; set; }

        /// <summary> 行動制御データ </summary>
        public IReadOnlyList<Behavior> Behaviors { get; set; }
    }
}
