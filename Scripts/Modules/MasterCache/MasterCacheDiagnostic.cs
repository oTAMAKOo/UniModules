
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
	public class MasterCacheDiagnostic : Singleton<MasterCacheDiagnostic>
    {
        //----- params -----

        //----- field -----

        protected Dictionary<string, double> dictionary = null;

        //----- property -----

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

        public string BuildLog()
        {
            if (dictionary.IsEmpty()) { return string.Empty; }

            var builder = new StringBuilder();

            foreach (var item in dictionary)
            {
                builder.AppendLine(string.Format("{0} ({1:F1}ms)", item.Key, item.Value));
            }

            return builder.ToString();
        }
    }
}
