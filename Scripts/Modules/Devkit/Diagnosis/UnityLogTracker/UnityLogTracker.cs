
using UnityEngine;
using System;
using System.Collections.Generic;
using R3;
using Extensions;
using Modules.Devkit.LogHandler;

namespace Modules.Devkit.Diagnosis.LogTracker
{
    [Serializable]
    public sealed class LogEntry
    {
        public LogType type = default;
        public string message = null;
        public string stackTrace = null;
    }

    public sealed class UnityLogTracker : Singleton<UnityLogTracker>
    {
        //----- params -----

        private const int DefaultReportLogNum = 100;

        //----- field -----

        private static FixedQueue<LogEntry> reportQueue = null;

        private static LifetimeDisposable disposable = null;

        private static bool initialized = false;

        //----- property -----

        public int ReportLogNum
        {
            get { return reportQueue.Length; }

            set
            {
                if (value <= 0) { return; }

                reportQueue = new FixedQueue<LogEntry>(value);
            }
        }

        public IReadOnlyList<LogEntry> Logs { get { return reportQueue.ToArray(); } }

        //----- method -----
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            Instance.Initialize();
        }

        private void Initialize()
        {
            var applicationLogHandler = ApplicationLogHandler.Instance;
            
            if (initialized) { return; }

            disposable = new LifetimeDisposable();

            reportQueue = new FixedQueue<LogEntry>(DefaultReportLogNum);

            applicationLogHandler.OnReceivedThreadedAllAsObservable()
                .Subscribe(x => LogCallback(x))
                .AddTo(disposable.Disposable);

            initialized = true;
        }

        private void LogCallback(ApplicationLogHandler.LogInfo log)
        {
            if (log == null) { return; }

            var item = new LogEntry()
            {
                type = log.Type,
                message = log.Condition,
                stackTrace = log.StackTrace,
            };

            reportQueue.Enqueue(item);
        }
    }
}
