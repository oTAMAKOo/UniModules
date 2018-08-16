﻿﻿
using Extensions;
using UnityEngine;
using UniRx;

namespace Modules.Devkit.Log
{
    public class ApplicationLogHandler : Singleton<ApplicationLogHandler>
    {
        //----- params -----

        public class LogInfo
        {
            public LogType Type { get; private set; }
            public string Condition { get; private set; }
            public string StackTrace { get; private set; }

            public LogInfo(string condition, string stackTrace, LogType logType)
            {
                Condition = condition;
                StackTrace = stackTrace;
                Type = logType;
            }
        }

        //----- field -----

        private Subject<LogInfo> onLogReceive = null;

        private Subject<Unit> onReceiveWarning = null;
        private Subject<Unit> onReceiveError = null;
        private Subject<Unit> onReceiveException = null;

        //----- property -----

        //----- method -----

        public ApplicationLogHandler()
        {
            Application.logMessageReceived += OnLogReceived;
        }

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (onLogReceive != null)
            {
                onLogReceive.OnNext(new LogInfo(condition, stackTrace, type));
            }

            switch (type)
            {
                case LogType.Warning:

                    if(onReceiveWarning != null)
                    {
                        onReceiveWarning.OnNext(Unit.Default);
                    }

                    break;
                
                case LogType.Error:

                    if (onReceiveError != null)
                    {
                        onReceiveError.OnNext(Unit.Default);
                    }

                    break;

                case LogType.Exception:

                    if (onReceiveException != null)
                    {
                        onReceiveException.OnNext(Unit.Default);
                    }

                    break;
            }
        }

        public IObservable<LogInfo> OnLogReceiveAsObservable()
        {
            return onLogReceive ?? (onLogReceive = new Subject<LogInfo>());
        }

        public IObservable<Unit> OnReceiveWarningAsObservable()
        {
            return onReceiveWarning ?? (onReceiveWarning = new Subject<Unit>());
        }

        public IObservable<Unit> OnReceiveErrorAsObservable()
        {
            return onReceiveError ?? (onReceiveError = new Subject<Unit>());
        }

        public IObservable<Unit> OnReceiveExceptionAsObservable()
        {
            return onReceiveException ?? (onReceiveException = new Subject<Unit>());
        }
    }
}