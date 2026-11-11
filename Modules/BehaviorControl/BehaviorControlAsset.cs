
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using Extensions;

namespace Modules.BehaviorControl
{
    public sealed class BehaviorControlAsset : ScriptableObject
    {
        //----- params -----

        [Serializable]
        public sealed class Condition
        {
            [SerializeField]
            private string type = null;
            [SerializeField]
            private string parameters = null;
            [SerializeField]
            private ConditionConnecter connecter = ConditionConnecter.None;

            public string Type { get { return type; } }

            public string Parameters { get { return parameters; } }

            public ConditionConnecter Connecter { get { return connecter; } }

            public Condition(string type, string parameters, ConditionConnecter connecter)
            {
                this.type = type;
                this.parameters = parameters;
                this.connecter = connecter;
            }
        }

        [Serializable]
        public sealed class Behavior
        {
            [SerializeField]
            private float successRate = 0;
            [SerializeField]
            private string actionType = null;
            [SerializeField]
            private string actionParameters = null;
            [SerializeField]
            private string targetType = null;
            [SerializeField]
            private string targetParameters = null;
            [SerializeField]
            private Condition[] conditions = null;

            public float SuccessRate { get { return successRate; } }

            public string ActionType { get { return actionType; } }

            public string ActionParameters { get { return actionParameters; } }

            public string TargetType { get { return targetType; } }

            public string TargetParameters { get { return targetParameters; } }

            public Condition[] Conditions { get { return conditions; } }

            public Behavior(float successRate, string actionType, string actionParameters, string targetType, string targetParameters, Condition[] conditions)
            {
                this.successRate = successRate;
                this.actionType = actionType;
                this.actionParameters = actionParameters;
                this.targetType = targetType;
                this.targetParameters = targetParameters;
                this.conditions = conditions;
            }
        }

        //----- field -----

        [SerializeField, ReadOnly]
        private string lastUpdate = null;

        [SerializeField, ReadOnly]
        private string description = null;

        [SerializeField, ReadOnly]
        private Behavior[] behaviors = null;

        //----- property -----

        public DateTime? LastUpdate
        {
            get
            {
                var style = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

                return string.IsNullOrEmpty(lastUpdate) ? null : (DateTime?)DateTime.Parse(lastUpdate, null, style);
            }
        }

        public string Description { get { return description; } }

        public IReadOnlyList<Behavior> Behaviors { get { return behaviors; } }

        //----- method -----

        public void Set(string description, Behavior[] behaviors, DateTime lastUpdate)
        {
            this.description = description;
            this.behaviors = behaviors;

            this.lastUpdate = lastUpdate.ToString();
        }
    }
}
