
using UnityEngine;

namespace Extensions
{
    public sealed class DisableStackTraceScope : Scope
    {
        //----- params -----

        //----- field -----

        private LogType logType = LogType.Log;
        private StackTraceLogType stackTraceLogType = StackTraceLogType.None;

        //----- property -----

        //----- method -----

        public DisableStackTraceScope(LogType logType)
        {
            this.logType = logType;

            stackTraceLogType = Application.GetStackTraceLogType(logType);

            Application.SetStackTraceLogType(logType, StackTraceLogType.None);
        }

        protected override void CloseScope()
        {
            Application.SetStackTraceLogType(logType, stackTraceLogType);
        }
    }
}
