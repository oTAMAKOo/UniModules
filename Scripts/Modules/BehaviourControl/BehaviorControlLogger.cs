
using System;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.BehaviorControl
{
    public sealed class BehaviorControlLogger : Singleton<BehaviorControlLogger>
    {
        //----- params -----

        //----- field -----

        private FixedQueue<LogData> logs = null;

        private Subject<Unit> onLogUpdate = null;

        //----- property -----

        public IReadOnlyList<LogData> Logs { get { return logs.ToArray(); } }

        //----- method -----
        
        protected override void OnCreate()
        {
            logs = new FixedQueue<LogData>(50);
        }

        public void Add(LogData log)
        {
            var controllerName = log.ControllerName;

            if (string.IsNullOrEmpty(controllerName)){ return; }

            logs.Enqueue(log);

            if (onLogUpdate != null)
            {
                onLogUpdate.OnNext(Unit.Default);
            }
        }

        public void Clear()
        {
            logs.Clear();

            if (onLogUpdate != null)
            {
                onLogUpdate.OnNext(Unit.Default);
            }
        }

        public IObservable<Unit> OnLogUpdateAsObservable()
        {
            return onLogUpdate ?? (onLogUpdate = new Subject<Unit>());
        }
    }
}
