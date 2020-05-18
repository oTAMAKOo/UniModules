
#if ENABLE_SRDEBUGGER

using System;
using UnityEngine;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Log;

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    [Serializable]
    public sealed class LogEntry
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public LogType LogType { get; private set; }
        public string Message { get; private set; }
        public string StackTrace { get; private set; }

        //----- method -----

        public LogEntry(LogType logType, string message, string stackTrace)
        {
            LogType = logType;
            Message = message;
            StackTrace = stackTrace;
        }
    }

    public static class SRTrackLogService
    {
        //----- params -----

        private const int DefaultReportLogNum = 30;

        //----- field -----

        private static FixedQueue<LogEntry> reportQueue = null;

        private static LifetimeDisposable disposable = null;

        private static bool initialized = false;

        //----- property -----

        public static int ReportLogNum
        {
            get { return reportQueue.Length; }

            set
            {
                if (value <= 0) { return; }

                reportQueue = new FixedQueue<LogEntry>(value);
            }
        }

        public static IReadOnlyList<LogEntry> Logs { get { return reportQueue.ToArray(); } }

        //----- method -----

        public static void Initialize()
        {
            if (initialized) { return; }

            disposable = new LifetimeDisposable();

            reportQueue = new FixedQueue<LogEntry>(DefaultReportLogNum);

            ApplicationLogHandler.Instance.OnLogReceiveAsObservable()
                .Subscribe(x => LogCallback(x))
                .AddTo(disposable.Disposable);

            initialized = true;
        }

        private static void LogCallback(ApplicationLogHandler.LogInfo log)
        {
            if (log == null) { return; }

            var item = new LogEntry(log.Type, log.Condition, log.StackTrace);

            reportQueue.Enqueue(item);
        }
    }    
}

#endif
