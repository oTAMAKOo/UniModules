
using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using UniRx;

namespace Modules.TimeLine.Component
{
    /// <summary> イベントタイプ </summary>
    public enum EventType
    {
        Enter,
        Stay,
        Exit,
    }

    [Serializable]
    public sealed class EventMethod : LifetimeDisposable
    {
        //----- params -----

        private static Type[] SupportArgumentTypes =
        {
            typeof(string),
            typeof(float),
            typeof(int),
            typeof(bool),
            typeof(Enum),
            typeof(GameObject),
        };

        [Serializable]
        public sealed class ArgumentObjects : LifetimeDisposable
        {
            [SerializeField]
            private ExposedReference<GameObject> targetObject;

            public ExposedReferenceResolver<GameObject> TargetObject { get; private set; }

            public void Setup(PlayableDirector playableDirector)
            {
                TargetObject = new ExposedReferenceResolver<GameObject>(playableDirector, targetObject);

                TargetObject.OnUpdateReferenceAsObservable()
                            .Subscribe(x => targetObject = x)
                            .AddTo(Disposable);
            }
        }

        //----- field -----

        [SerializeField]
        private ExposedReference<GameObject> invokeTarget;
        [SerializeField]
        private EventType eventType = EventType.Enter;
        [SerializeField]
        private string methodName = null;
        [SerializeField]
        private string[] argumentValues = null;
        [SerializeField]
        private ArgumentObjects[] argumentObjects = null;

        private MethodInfo methodInfo = null;
        private object[] parameters = null;

        [NonSerialized]
        private bool build = false;

        //----- property -----

        public ExposedReferenceResolver<GameObject> InvokeTarget { get; private set; }

        /// <summary> イベント発行タイプ </summary>
        public EventType EventType
        {
            get { return eventType; }
            set { eventType = value; }
        }

        /// <summary> 呼び出し関数 </summary>
        public string MethodName
        {
            get { return methodName; }
            set { methodName = value; }
        }

        public string[] ValueArguments
        {
            get { return argumentValues; }
            set { argumentValues = value; }
        }

        public ArgumentObjects[] ObjectArguments
        {
            get { return argumentObjects; }
            set { argumentObjects = value; }
        }

        //----- method -----

        public void Setup(PlayableDirector playableDirector)
        {
            InvokeTarget = new ExposedReferenceResolver<GameObject>(playableDirector, invokeTarget);

            InvokeTarget.OnUpdateReferenceAsObservable()
                        .Subscribe(x => invokeTarget = x)
                        .AddTo(Disposable);

            if (argumentObjects != null)
            {
                foreach (var argumentObject in argumentObjects)
                {
                    argumentObject.Setup(playableDirector);
                }
            }
        }

        public void Clear()
        {
            InvokeTarget.Clear();

            if (argumentObjects != null)
            {
                foreach (var argumentObject in argumentObjects)
                {
                    argumentObject.TargetObject.Clear();
                }
            }
        }

        private void Build()
        {
            if (build) { return; }

            methodInfo = null;
            parameters = null;

            var target = InvokeTarget.GetValue();

            if (target == null || string.IsNullOrEmpty(methodName)) { return; }

            var methods = GetTimeLineEventMethods(target);

            methodInfo = methods.FirstOrDefault(x => GetMethodFullName(x) == methodName);

            if (methodInfo == null)
            {
                Debug.LogErrorFormat("Failed build method timeline event.\nMethod not found : {0}", methodName);
                return;
            }

            var args = new List<object>();

            try
            {
                var methodParameters = methodInfo.GetParameters();

                var valueArgIndex = 0;
                var objectArgIndex = 0;

                for (var i = 0; i < methodParameters.Length; i++)
                {
                    var param = methodParameters[i];

                    var paramType = param.ParameterType;

                    if (paramType == typeof(GameObject))
                    {
                        GameObject go = null;

                        go = argumentObjects[objectArgIndex].TargetObject.GetValue();

                        args.Add(go);
                        objectArgIndex++;
                    }
                    else
                    {
                        var value = argumentValues[valueArgIndex];

                        if (paramType.IsEnum)
                        {
                            args.Add(value.To<int>());
                        }
                        else if (SupportArgumentTypes.Contains(paramType))
                        {
                            if (paramType == typeof(string))
                            {
                                args.Add(value);
                            }
                            else if (paramType == typeof(int))
                            {
                                args.Add(value.To<int>());
                            }
                            else if (paramType == typeof(float))
                            {
                                args.Add(value.To<float>());
                            }
                            else if (paramType == typeof(bool))
                            {
                                args.Add(value.To<bool>());
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }

                        valueArgIndex++;
                    }                    
                }

                parameters = args.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Failed build method timeline event.\nMethod arguments error : {0}\n\n{1}", methodName, e);
            }

            build = true;
        }

        public void Invoke()
        {
            Build();

            if (methodInfo == null || parameters == null) { return; }

            var targetGameObject = InvokeTarget.GetValue();

            if (targetGameObject != null)
            {
                var target = targetGameObject.GetComponent(methodInfo.ReflectedType);

                if (target != null)
                {
                    methodInfo.Invoke(target, parameters);
                }
            }
        }

        public static string GetMethodFullName(MethodInfo methodInfo)
        {
            if (methodInfo == null) { return string.Empty; }

            return methodInfo.DeclaringType + "." + methodInfo.Name;
        }

        // 下記条件を満たす関数一覧を取得.
        // ・[TimeLineEvent]アトリビュートが付いている.
        // ・public
        // ・戻り値がvoid
        // ・引数の型がstring / int / float/ bool/ GameObjectのみを使用している.
        public static IEnumerable<MethodInfo> GetTimeLineEventMethods(GameObject target)
        {
            if (target == null) { return new MethodInfo[0]; }

            var behaviours = target.GetComponents<Behaviour>();

            var types = behaviours.Select(x => x.GetType()).ToArray();

            return types.SelectMany(x =>
                {
                    var list = new List<MethodInfo>();

                    var methods = x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToArray();

                    foreach (var method in methods)
                    {
                        if (method.GetCustomAttributes(typeof(TimeLineEventAttribute), true).IsEmpty()) { continue; }

                        if (method.ReturnType != typeof(void)) { continue; }

                        var parameters = method.GetParameters();

                        var valid = true;

                        if (parameters.Any())
                        {
                            foreach (var parameter in parameters)
                            {
                                valid = parameter.ParameterType == typeof(string) ||
                                        parameter.ParameterType == typeof(int) ||
                                        parameter.ParameterType == typeof(float) ||
                                        parameter.ParameterType == typeof(bool) ||
                                        parameter.ParameterType == typeof(GameObject) ||
                                        parameter.ParameterType.IsEnum;

                                if (!valid) { break; }
                            }
                        }

                        if (valid)
                        {
                            list.Add(method);
                        }
                    }

                    return list;
                })
            .ToArray();
        }
    }
}
