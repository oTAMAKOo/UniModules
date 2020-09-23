
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.BehaviorControl
{
    public sealed class BehaviorController<TArgument, TAction, TTarget, TCondition> where TAction : Enum where TTarget : Enum where TCondition : Enum
    {
        //----- params -----

        public delegate bool ExecuteAction(ref TArgument argument, Parameter parameter);

        public delegate bool SelectTarget(ref TArgument argument, Parameter parameter);

        public delegate bool CheckCondition(ref TArgument argument, Parameter parameter);

        //----- field -----

        private string controllerName = null;

        private Dictionary<TAction, ExecuteAction> executeActionCallbacks = null;

        private Dictionary<TTarget, SelectTarget> selectTargetCallbacks = null;

        private Dictionary<TCondition, CheckCondition> checkConditionCallbacks = null;

        private Action<string> onErrorCallback = null;

        //----- property -----

        //----- method -----

        public BehaviorController(string controllerName, Action<string> onErrorCallback = null)
        {
            this.controllerName = controllerName;
            this.onErrorCallback = onErrorCallback;

            executeActionCallbacks = new Dictionary<TAction, ExecuteAction>();
            selectTargetCallbacks = new Dictionary<TTarget, SelectTarget>();
            checkConditionCallbacks = new Dictionary<TCondition, CheckCondition>();
        }
        
        public bool Execute(string behaviorName, BehaviorData<TAction, TTarget, TCondition> behaviorData, TArgument argument)
        {
            var result = false;

            if (behaviorData == null){ return false; }

            var logData = new LogData(controllerName, behaviorName);
            
            foreach (var behavior in behaviorData.Behaviors)
            {
                var probability = behavior.SuccessRate * 100f;

                //====== ログデータ構築 ======

                var actionNode = new LogData.Node() { Type = behavior.ActionType.ToString(), Parameter = behavior.ActionParameters };

                var targetNode = new LogData.Node() { Type = behavior.TargetType.ToString(), Parameter = behavior.TargetParameters };

                var conditionNodes = behavior.Conditions
                    .Select(x =>
                        {
                            return new LogData.Node()
                            {
                                Type = x.Type.ToString(),
                                Parameter = x.Parameters,
                            };
                        })
                    .ToArray();

                var connecters = behavior.Conditions.Select(x => x.Connecter).ToArray();

                var logElement = new LogData.Element()
                {
                    Probability = probability,
                    ActionNode = actionNode,
                    TargetNode = targetNode,
                    ConditionNodes = conditionNodes,
                    Connecters = connecters,
                };

                //====== 確率判定 ======

                var percentage = RandomUtility.RandomInRange(1f, 100f);

                var isHit = percentage != 0 && percentage <= probability;

                logElement.Percentage = percentage;

                if (!isHit) { continue; }

                //====== 対象検索 ======

                var selectResult = InvokeTargetSelectCallback(ref argument, behavior);

                logElement.TargetNode.Result = selectResult;

                if (!selectResult) { continue; }

                //====== 条件一致判定 ======
                
                var connecter = ConditionConnecter.None;

                var check = true;

                for (var i = 0; i < behavior.Conditions.Length; i++)
                {
                    var condition = behavior.Conditions[i];

                    var conditionResult = InvokeCheckConditionCallback(ref argument, condition);

                    // 複数条件は接続詞が必要.
                    if (0 < i)
                    {
                        connecter = condition.Connecter;

                        // 接続詞が未定義.
                        if (connecter == ConditionConnecter.None) { break; }
                    }

                    switch (connecter)
                    {
                        case ConditionConnecter.And:
                            check &= conditionResult;
                            break;

                        case ConditionConnecter.Or:
                            check |= conditionResult;
                            break;

                        case ConditionConnecter.None:
                            check = conditionResult;
                            break;
                    }

                    logElement.ConditionNodes[i].Result = conditionResult;

                    if (!check) { break; }
                }

                //====== 行動実行 ======

                if (check)
                {
                    var actionResult = InvokeActionCallback(ref argument, behavior);

                    logElement.ActionNode.Result = actionResult;

                    // 行動実行したら終了.
                    if (actionResult)
                    {
                        result = true;
                    }
                }

                logData.AddElement(logElement);

                if (result) { break; }
            }

            BehaviorControlLogger.Instance.Add(logData);

            return result;
        }

        public void RegisterCallback(TAction type, ExecuteAction callback)
        {
            executeActionCallbacks[type] = callback;
        }

        public void RegisterCallback(TTarget type, SelectTarget callback)
        {
            selectTargetCallbacks[type] = callback;
        }

        public void RegisterCallback(TCondition type, CheckCondition callback)
        {
            checkConditionCallbacks[type] = callback;
        }

        private bool InvokeActionCallback(ref TArgument argument, BehaviorData<TAction, TTarget, TCondition>.Behavior behavior)
        {
            var callback = executeActionCallbacks.GetValueOrDefault(behavior.ActionType);

            if (callback == null)
            {
                if (onErrorCallback != null)
                {
                    var message = string.Format("InvokeActionCallback: Callback not found.\n{0} => {1}", typeof(TAction).Name, behavior.ActionType);

                    onErrorCallback.Invoke(message);
                }

                return false;
            }

            var contents = ParseParameters(behavior.ActionParameters);

            var parameter = new Parameter(contents, onErrorCallback);
            
            return callback.Invoke(ref argument, parameter);
        }

        private bool InvokeTargetSelectCallback(ref TArgument argument, BehaviorData<TAction, TTarget, TCondition>.Behavior behavior)
        {
            var callback = selectTargetCallbacks.GetValueOrDefault(behavior.TargetType);

            if (callback == null)
            {
                if (onErrorCallback != null)
                {
                    var message = string.Format("InvokeTargetSelectCallback: Callback not found.\n{0} => {1}", typeof(TTarget).Name, behavior.TargetType);

                    onErrorCallback.Invoke(message);
                }

                return false;
            }

            var contents = ParseParameters(behavior.TargetParameters);

            var parameter = new Parameter(contents, onErrorCallback);
            
            return callback.Invoke(ref argument, parameter);
        }

        private bool InvokeCheckConditionCallback(ref TArgument argument, BehaviorData<TAction, TTarget, TCondition>.Condition condition)
        {
            var callback = checkConditionCallbacks.GetValueOrDefault(condition.Type);

            if (callback == null)
            {
                if (onErrorCallback != null)
                {
                    var message = string.Format("InvokeCheckConditionCallback: Callback not found.\n{0} => {1}", typeof(TCondition).Name, condition.Type);

                    onErrorCallback.Invoke(message);
                }

                return false;
            }

            var contents = ParseParameters(condition.Parameters);

            var parameter = new Parameter(contents, onErrorCallback);
            
            return callback.Invoke(ref argument, parameter);
        }

        private static string[] ParseParameters(string str)
        {
            return str.Split(',').Select(x => x.Trim()).ToArray();
        }
    }
}
