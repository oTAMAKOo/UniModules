
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Extensions;

namespace Modules.BehaviorControl
{
    public sealed class BehaviorDataBuilder<TBehaviorData, TAction, TTarget, TCondition>
        where TBehaviorData : BehaviorData<TAction, TTarget, TCondition>, new()
        where TAction : Enum where TTarget : Enum where TCondition : Enum
    {
        //----- params -----

        //----- field -----

        private Dictionary<string, TAction> actionEnums = null;
        private Dictionary<string, TTarget> targetEnums = null;
        private Dictionary<string, TCondition> conditionEnums = null;

        private Action<string> onErrorCallback = null;

        //----- property -----

        //----- method -----

        public BehaviorDataBuilder(Action<string> onErrorCallback = null)
        {
            this.onErrorCallback = onErrorCallback;

            actionEnums = CreateEnumDictionary<TAction>();
            targetEnums = CreateEnumDictionary<TTarget>();
            conditionEnums = CreateEnumDictionary<TCondition>();
        }

        public TBehaviorData Build(BehaviorControlAsset behaviorControlAsset)
        {
            if (behaviorControlAsset == null){ return null; }

            var hasError = false;
            var errorMessage = new StringBuilder();

            errorMessage.AppendFormat("Build Error").AppendLine();
            errorMessage.AppendLine();

            var behaviors = new List<BehaviorData<TAction, TTarget, TCondition>.Behavior>();

            for (var i = 0; i < behaviorControlAsset.Behaviors.Count; i++)
            {
                var recordError = new StringBuilder();

                var content = behaviorControlAsset.Behaviors[i];

                if (content == null) { continue; }

                //------ ActionType ------

                var actionType = default(TAction);

                if (!string.IsNullOrEmpty(content.ActionType) && actionEnums.ContainsKey(content.ActionType))
                {
                    actionType = actionEnums.GetValueOrDefault(content.ActionType);
                }
                else
                {
                    var message = string.Format("Unknown action type. {0} = {1}", typeof(TAction).Name, content.ActionType);

                    recordError.AppendLine(message);
                }

                //------ TargetType ------

                var targetType = default(TTarget);

                if (!string.IsNullOrEmpty(content.TargetType) && targetEnums.ContainsKey(content.TargetType))
                {
                    targetType = targetEnums.GetValueOrDefault(content.TargetType);
                }
                else
                {
                    var message = string.Format("Unknown target type. {0} = {1}", typeof(TTarget).Name, content.TargetType);

                    recordError.AppendLine(message);
                }

                //------ Conditions ------

                var conditions = new List<BehaviorData<TAction, TTarget, TCondition>.Condition>();

                foreach (var item in content.Conditions)
                {
                    var conditionType = default(TCondition);

                    if (!string.IsNullOrEmpty(item.Type) && conditionEnums.ContainsKey(item.Type))
                    {
                        conditionType = conditionEnums.GetValueOrDefault(item.Type);
                    }
                    else
                    {
                        var message = string.Format("Unknown condition type. {0} = {1}", typeof(TCondition).Name, item.Type);

                        recordError.AppendLine(message);
                    }

                    var condition = new BehaviorData<TAction, TTarget, TCondition>.Condition
                    {
                        Connecter = item.Connecter,
                        Type = conditionType,
                        Parameters = item.Parameters,
                    };

                    conditions.Add(condition);
                }

                //------------------------

                if (recordError.Length == 0)
                {
                    var behavior = new BehaviorData<TAction, TTarget, TCondition>.Behavior
                    {
                        ActionType = actionType,
                        TargetType = targetType,
                        Conditions = conditions.ToArray(),

                        SuccessRate = content.SuccessRate,
                        ActionParameters = content.ActionParameters,
                        TargetParameters = content.TargetParameters,
                    };

                    behaviors.Add(behavior);
                }
                else
                {
                    errorMessage.AppendFormat("----- Record:{0} -----", i).AppendLine();
                    errorMessage.AppendLine();
                    errorMessage.AppendLine(recordError.ToString());

                    hasError = true;
                }
            }

            if (hasError)
            {
                if (onErrorCallback != null)
                {
                    onErrorCallback.Invoke(errorMessage.ToString());
                }

                return null;
            }

            var behaviorData = new TBehaviorData()
            {
                Description = behaviorControlAsset.Description,
                Behaviors = behaviors.ToArray(),
            };

            return behaviorData;
        }

        private static Dictionary<string, TEnum> CreateEnumDictionary<TEnum>() where TEnum : Enum
        {
            var dictionary = new Dictionary<string, TEnum>();

            var names = Enum.GetNames(typeof(TEnum));
            var values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToArray();

            for (var i = 0; i < names.Length; i++)
            {
                dictionary.Add(names[i], values[i]);
            }

            return dictionary;
        }
    }
}
