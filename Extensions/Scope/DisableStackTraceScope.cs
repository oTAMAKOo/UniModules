
using UnityEngine;
using System.Collections.Generic;

namespace Extensions
{
    public sealed class DisableStackTraceScope : Scope
    {
        //----- params -----

        private static readonly LogType[] DefaultTargetLogTypes = new LogType[]
        {
            LogType.Log, LogType.Warning, LogType.Error, LogType.Assert,
        };

        //----- field -----

        private Dictionary<LogType, StackTraceLogType> resumeDictionary = null;

        //----- property -----

        //----- method -----

        public DisableStackTraceScope()
        {
            resumeDictionary = new Dictionary<LogType, StackTraceLogType>();

            foreach (var logType in DefaultTargetLogTypes)
            {
                var stackTraceLogType = Application.GetStackTraceLogType(logType);

                Application.SetStackTraceLogType(logType, StackTraceLogType.None);

                resumeDictionary.Add(logType, stackTraceLogType);
            }
        }

        public DisableStackTraceScope(LogType logType)
        {
            resumeDictionary = new Dictionary<LogType, StackTraceLogType>();

            var stackTraceLogType = Application.GetStackTraceLogType(logType);

            Application.SetStackTraceLogType(logType, StackTraceLogType.None);

            resumeDictionary.Add(logType, stackTraceLogType);
        }

        protected override void CloseScope()
        {
            foreach (var item in resumeDictionary)
            {
                Application.SetStackTraceLogType(item.Key, item.Value);
            }
        }
    }
}
