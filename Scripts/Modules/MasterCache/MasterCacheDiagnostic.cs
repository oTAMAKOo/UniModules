
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UniRx;
using Extensions;
using Modules.Devkit;

namespace Modules.MasterCache
{
	public abstract class MasterCacheDiagnostic<TInstance> : Singleton<TInstance> where TInstance : MasterCacheDiagnostic<TInstance>
    {
        //----- params -----

        //----- field -----

        protected Dictionary<string, double> dictionary = null;

        //----- property -----

        protected abstract string LogTitle { get; }

        //----- method -----

        protected MasterCacheDiagnostic()
        {
            dictionary = new Dictionary<string, double>();
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public void Register<T>(double time)
        {
            dictionary[typeof(T).Name] = time;
        }

        public string BuildLog(double totalTime)
        {
            if (dictionary.IsEmpty()) { return string.Empty; }

            var builder = new StringBuilder();

            builder.AppendLine(string.Format("---------------- {0} : ({1:F1}ms) ----------------", LogTitle, totalTime));

            builder.AppendLine();

            foreach (var item in dictionary)
            {
                builder.AppendLine(string.Format("{0} ({1:F1}ms)", item.Key, item.Value));
            }

            builder.AppendLine();

            builder.AppendLine("----------------------------------------");

            return builder.ToString();
        }
    }

    public class MasterCacheUpdateDiagnostic : MasterCacheDiagnostic<MasterCacheUpdateDiagnostic>
    {
        protected override string LogTitle { get { return "Update"; } }
    }

    public class MasterCacheLoadDiagnostic : MasterCacheDiagnostic<MasterCacheLoadDiagnostic>
    {
        protected override string LogTitle { get { return "Load"; } }
    }
}
