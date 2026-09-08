
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Extensions;

namespace Modules.BehaviorControl
{
    public sealed class ImportDataConverter<TBehaviorData, TAction, TTarget, TCondition>
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

        public ImportDataConverter(Action<string> onErrorCallback = null)
        {
            this.onErrorCallback = onErrorCallback;

            actionEnums = CreateEnumDictionary<TAction>();
            targetEnums = CreateEnumDictionary<TTarget>();
            conditionEnums = CreateEnumDictionary<TCondition>();
        }

        public TBehaviorData Convert(string dataPath, ImportData importData)
        {
            var hasError = false;
            var errorMessage = new StringBuilder();

            errorMessage.AppendFormat("Import Error").AppendLine();
            errorMessage.AppendFormat("File: {0}", dataPath).AppendLine();
            errorMessage.AppendFormat("Sheet: {0}", importData.sheetName).AppendLine();
            errorMessage.AppendLine();

            var behaviors = new List<BehaviorData<TAction, TTarget, TCondition>.Behavior>();

            for (var i = 0; i < importData.records.Length; i++)
            {
                var recordError = new StringBuilder();

                var record = importData.records[i];

                if (record.behavior == null) { continue; }

                //------ ActionType ------

                var actionType = default(TAction);

                if (!string.IsNullOrEmpty(record.behavior.actionType) && actionEnums.ContainsKey(record.behavior.actionType))
                {
                    actionType = actionEnums.GetValueOrDefault(record.behavior.actionType);
                }
                else
                {
                    var message = string.Format("Unknown action type. {0} = {1}", typeof(TAction).Name, record.behavior.actionType);

                    recordError.AppendLine(message);
                }

                //------ TargetType ------

                var targetType = default(TTarget);

                if (!string.IsNullOrEmpty(record.behavior.targetType) && targetEnums.ContainsKey(record.behavior.targetType))
                {
                    targetType = targetEnums.GetValueOrDefault(record.behavior.targetType);
                }
                else
                {
                    var message = string.Format("Unknown target type. {0} = {1}", typeof(TTarget).Name, record.behavior.targetType);

                    recordError.AppendLine(message);
                }

                //------ Conditions ------

                var conditions = new List<BehaviorData<TAction, TTarget, TCondition>.Condition>();

                foreach (var item in record.behavior.conditions)
                {
                    var conditionType = default(TCondition);

                    if (!string.IsNullOrEmpty(item.type) && conditionEnums.ContainsKey(item.type))
                    {
                        conditionType = conditionEnums.GetValueOrDefault(item.type);
                    }
                    else
                    {
                        var message = string.Format("Unknown condition type. {0} = {1}", typeof(TCondition).Name, item.type);

                        recordError.AppendLine(message);
                    }

                    var connecter = ConditionConnecter.None;

                    if (conditions.Any())
                    {
                        switch (item.connecter)
                        {
                            case "&":
                                connecter = ConditionConnecter.And;
                                break;

                            case "|":
                                connecter = ConditionConnecter.Or;
                                break;
                        }

                        if (connecter == ConditionConnecter.None)
                        {
                            var message = string.Format("Unknown connecter type. [{0}]", item.connecter);

                            recordError.AppendLine(message);
                        }
                    }

                    var condition = new BehaviorData<TAction, TTarget, TCondition>.Condition
                    {
                        Connecter = connecter,
                        Type = conditionType,
                        Parameters = item.parameters,
                    };

                    conditions.Add(condition);
                }

                //------------------------

                if (recordError.Length == 0)
                {
                    var behavior = new BehaviorData<TAction, TTarget, TCondition>.Behavior()
                    {
                        ActionType = actionType,
                        TargetType = targetType,
                        Conditions = conditions.ToArray(),

                        SuccessRate = record.behavior.successRate,
                        ActionParameters = record.behavior.actionParameters,
                        TargetParameters = record.behavior.targetParameters,
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
                Description = importData.description,
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
