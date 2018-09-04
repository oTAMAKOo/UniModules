
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Extensions;

namespace Modules.TimeLine.Component
{
    public class EventMethodInfo
    {
        //----- params -----

        public class ArgumentInfo
        {
            public ParameterInfo info = null;
            public object value = null;
        }

        //----- field -----

        //----- property -----

        // 呼び出し対象.
        public GameObject InvokeTarget { get; private set; }

        // 呼び出しタイミング.
        public EventType EventType { get; private set; }

        // 関数名.
        public string MethodName { get; private set; }

        // 引数情報.
        public ArgumentInfo[] Arguments { get; private set; }

        // 関数データキャッシュ.
        public string[] CallbackMethods { get; private set; }
        public string[] CallbackMethodNames { get; private set; }

        //----- method -----

        public void SetInvokeTarget(GameObject invokeTarget)
        {
            InvokeTarget = invokeTarget;

            var methods = EventMethod.GetTimeLineEventMethods(InvokeTarget).ToArray();
            
            CallbackMethods = GetCallbackMethods(methods);
            CallbackMethodNames = GetCallbackMethodNames(CallbackMethods);

            MethodName = null;
            Arguments = new ArgumentInfo[0];
        }

        public void SetArguments(string[] valueArguments, EventMethod.ArgumentObjects[] objectArguments)
        {
            var methodInfo = GetMethodInfo(InvokeTarget, MethodName);

            if (methodInfo == null) { return; }

            var list = new List<ArgumentInfo>();

            var parameters = methodInfo.GetParameters();

            var objectArgIndex = 0;
            var valueArgIndex = 0;

            var valueTypes = new Type[]
            {
                typeof(string),
                typeof(int),
                typeof(float),
                typeof(bool),
            };

            foreach (var parameter in parameters)
            {
                var type = parameter.ParameterType;

                object value = null;

                if (type == typeof(GameObject))
                {
                    value = objectArguments[objectArgIndex].TargetObject.GetValue();

                    list.Add(new ArgumentInfo() { info = parameter, value = value });
                    objectArgIndex++;
                }
                else if(valueTypes.Contains(type))
                {
                    if (type == typeof(string))
                    {
                        value = valueArguments[valueArgIndex];
                    }
                    else if (type == typeof(int))
                    {
                        value = valueArguments[valueArgIndex].To<int>();
                    }
                    else if (type == typeof(float))
                    {
                        value = valueArguments[valueArgIndex].To<float>();
                    }
                    else if (type == typeof(bool))
                    {
                        value = valueArguments[valueArgIndex].To<bool>();
                    }
                    else if (type.IsEnum)
                    {
                        value = valueArguments[valueArgIndex].To<int>();
                    }

                    list.Add(new ArgumentInfo() { info = parameter, value = value });
                    valueArgIndex++;
                }
            }

            Arguments = list.ToArray();
        }

        public void SetMethodName(string methodName)
        {
            var methodInfo = GetMethodInfo(InvokeTarget, methodName);

            if (methodInfo == null) { return; }

            MethodName = EventMethod.GetMethodFullName(methodInfo);

            // 引数構築.

            var arguments = new List<ArgumentInfo>();

            if (methodInfo != null)
            {
                var parameters = methodInfo.GetParameters();

                foreach (var parameter in parameters)
                {
                    var argument = new ArgumentInfo() { info = parameter };
                    arguments.Add(argument);
                }
            }

            Arguments = arguments.ToArray();
        }

        public void SetEventType(EventType eventType)
        {
            EventType = eventType;
        }

        private static MethodInfo GetMethodInfo(GameObject invokeTarget, string methodName)
        {
            if(invokeTarget == null) { return null; }

            var methods = EventMethod.GetTimeLineEventMethods(invokeTarget).ToArray();

            var methodInfo = methods.FirstOrDefault(x => EventMethod.GetMethodFullName(x) == methodName);

            return methodInfo;
        }

        private static string[] GetCallbackMethods(MethodInfo[] methods)
        {
            if (methods == null || methods.IsEmpty()) { return new string[0]; }

            return methods.Select(x => EventMethod.GetMethodFullName(x)).ToArray();
        }

        private static string[] GetCallbackMethodNames(string[] callbackMethods)
        {
            if (callbackMethods.IsEmpty()) { return null; }

            var lastTwoDotPattern = @"[^\.]+\.[^\.]+$";

            var methodNames = callbackMethods
                .Select(m =>
                    {
                        var result = Regex.Match(m, lastTwoDotPattern, RegexOptions.RightToLeft);
                        return result.Success ? result.Value : m;
                    })
                .ToList();

            methodNames.Insert(0, "None");

            return methodNames.ToArray();
        }
    }
}
