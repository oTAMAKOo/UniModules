
#if ENABLE_SRDEBUGGER

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Log;

namespace Modules.Devkit.Diagnosis.SRDebugger
{
    public sealed class LogEntry
    {
        //----- params -----

        private const int MessagePreviewLength = 180;
        private const int StackTracePreviewLength = 120;

        //----- field -----

        //----- property -----

        public LogType LogType { get; private set; }
        public string Message { get; private set; }
        public string StackTrace { get; private set; }
        public string MessagePreview { get; private set; }
        public string StackTracePreview { get; private set; }

        //----- method -----

        public LogEntry(LogType logType, string message, string stackTrace)
        {
            LogType = logType;
            Message = message;
            StackTrace = stackTrace;

            MessagePreview = GetPreviewText(Message, MessagePreviewLength);
            StackTracePreview = GetPreviewText(StackTrace, StackTracePreviewLength);
        }

        private string GetPreviewText(string text, int maxLength)
        {
            var line = text.Split('\n')[0];

            return string.IsNullOrEmpty(line) ? string.Empty : line.Substring(0, Mathf.Min(line.Length, maxLength));
        }
    }

    public static class SRTrackLogService
    {
        //----- params -----

        public const int ReportLogNum = 30;

        //----- field -----

        private static FixedQueue<LogEntry> reportQueue = null;

        private static LifetimeDisposable disposable = null;

        private static bool initialized = false;

        //----- property -----

        public static LogEntry[] Logs
        {
            get
            {
                return reportQueue.ToArray();
            }
        }

        //----- method -----

        public static void Initialize()
        {
            if (initialized) { return; }

            disposable = new LifetimeDisposable();

            reportQueue = new FixedQueue<LogEntry>(ReportLogNum);

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
