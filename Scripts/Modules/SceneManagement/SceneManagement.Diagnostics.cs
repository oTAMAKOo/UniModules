
using UnityEngine;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Constants;
using UniRx;
using Extensions;

namespace Modules.SceneManagement.Diagnostics
{
    public sealed class TimeDiagnostics
    {
        //----- params -----

        public enum Measure
        {
            Load,
            Prepare,
            Leave,
            Total,
            Append,
        }

        //----- field -----

        private Dictionary<Measure, Stopwatch> stopwatch = null;
        private Dictionary<Measure, double> result = null;

        //----- property -----

        //----- method -----

        public TimeDiagnostics()
        {
            stopwatch = new Dictionary<Measure, Stopwatch>();
            result = new Dictionary<Measure, double>();
        }

        public void Begin(Measure type)
        {
            stopwatch[type] = Stopwatch.StartNew();
        }

        public void Finish(Measure type)
        {
            var sw = stopwatch.GetValueOrDefault(type);

            if (sw != null)
            {
                sw.Stop();

                stopwatch.Remove(type);
                result[type] = sw.Elapsed.TotalMilliseconds;
            }
        }

        public double? GetTime(Measure type)
        {
            var time = result.GetValueOrDefault(type, -1);

            return time == -1 ? null : (double?)time;
        }

        public string BuildDetailText()
        {
            var builder = new StringBuilder();

            builder.AppendLine("--------------------------------------");

            var sceneLeaveTime = GetTime(Measure.Leave);

            if (sceneLeaveTime.HasValue)
            {
                builder.AppendFormat("Leave    : {0:F2}ms", sceneLeaveTime.Value).AppendLine();
            }

            var sceneLoadTime = GetTime(Measure.Load);

            if (sceneLoadTime.HasValue)
            {
                builder.AppendFormat("Load      : {0:F2}ms", sceneLoadTime.Value).AppendLine();
            }

            var scenePrepareTime = GetTime(Measure.Prepare);

            if (scenePrepareTime.HasValue)
            {
                builder.AppendFormat("Prepare : {0:F2}ms", scenePrepareTime.Value).AppendLine();
            }

            builder.AppendLine("--------------------------------------");

            return builder.ToString();
        }
    }
}
