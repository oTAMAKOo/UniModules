﻿
using System;
using UnityEngine;
using UniRx;
using Extensions;

namespace Modules.Devkit.LogHandler
{
    public sealed class ApplicationLogHandler : Singleton<ApplicationLogHandler>
    {
        //----- params -----

        public sealed class LogInfo
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

        private Subject<LogInfo> onReceivedAll = null;

        private Subject<LogInfo> onReceivedLog = null;

        private Subject<LogInfo> onReceivedWarning = null;

        private Subject<LogInfo> onReceivedError = null;

        private Subject<LogInfo> onReceivedException = null;

        private Subject<LogInfo> onReceivedThreadedAll = null;

        private Subject<LogInfo> onReceivedThreadedLog = null;

        private Subject<LogInfo> onReceivedThreadedWarning = null;

        private Subject<LogInfo> onReceivedThreadedError = null;

        private Subject<LogInfo> onReceivedThreadedException = null;

        //----- property -----

        //----- method -----

        private ApplicationLogHandler()
        {
            Application.logMessageReceived += OnLogMessageReceived;

            Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            var logInfo = new LogInfo(condition, stackTrace, type);

            switch (type)
            {
                case LogType.Log:
                    onReceivedLog?.OnNext(logInfo);
                    break;

                case LogType.Warning:
                    onReceivedWarning?.OnNext(logInfo);
                    break;
                
                case LogType.Error:
                    onReceivedError?.OnNext(logInfo);
                    break;

                case LogType.Exception:
                    onReceivedException?.OnNext(logInfo);
                    break;
            }

            onReceivedAll?.OnNext(logInfo);
        }

        private void OnLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
        {
            var logInfo = new LogInfo(condition, stackTrace, type);

            switch (type)
            {
                case LogType.Log:
                    onReceivedThreadedLog?.OnNext(logInfo);
                    break;

                case LogType.Warning:
                    onReceivedThreadedWarning?.OnNext(logInfo);
                    break;
                
                case LogType.Error:
                    onReceivedThreadedError?.OnNext(logInfo);
                    break;

                case LogType.Exception:
                    onReceivedThreadedException?.OnNext(logInfo);
                    break;
            }

            onReceivedThreadedAll?.OnNext(logInfo);
        }

        public IObservable<LogInfo> OnReceivedAllAsObservable()
        {
            return onReceivedAll ?? (onReceivedAll = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedLogAsObservable()
        {
            return onReceivedLog ?? (onReceivedLog = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedWarningAsObservable()
        {
            return onReceivedWarning ?? (onReceivedWarning = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedErrorAsObservable()
        {
            return onReceivedError ?? (onReceivedError = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedExceptionAsObservable()
        {
            return onReceivedException ?? (onReceivedException = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedThreadedAllAsObservable()
        {
            return onReceivedThreadedAll ?? (onReceivedThreadedAll = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedThreadedLogAsObservable()
        {
            return onReceivedThreadedLog ?? (onReceivedThreadedLog = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedThreadedWarningAsObservable()
        {
            return onReceivedThreadedWarning ?? (onReceivedThreadedWarning = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedThreadedErrorAsObservable()
        {
            return onReceivedThreadedError ?? (onReceivedThreadedError = new Subject<LogInfo>());
        }

        public IObservable<LogInfo> OnReceivedThreadedExceptionAsObservable()
        {
            return onReceivedThreadedException ?? (onReceivedThreadedException = new Subject<LogInfo>());
        }
    }
}
