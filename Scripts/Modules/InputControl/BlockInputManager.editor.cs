
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Extensions;

namespace Modules.InputControl
{
    public sealed partial class BlockInputManager
    {
        //----- params -----
        
        //----- field -----

        private Dictionary<ulong, string> trackInputBlock = null;

        //----- property -----

        //----- method -----

        private void AddTracker(ulong blockingId)
        {
            if (trackInputBlock == null)
            {
                trackInputBlock = new Dictionary<ulong, string>();
            }

            // 実際の呼び出し元開始行数.
            const int StackTraceStartLine = 4;
            
            var stackTrace = StackTraceUtility.ExtractStackTrace();

            stackTrace = stackTrace.FixLineEnd();

            var lines = stackTrace.Split('\n').ToList();

            var builder = new StringBuilder();

            for (var i = 0; i < lines.Count; i++)
            {
                if (i < StackTraceStartLine){ continue; }

                builder.AppendLine(lines[i]);
            }

            stackTrace = builder.ToString().FixLineEnd().Trim();

            trackInputBlock[blockingId] = stackTrace;
        }

        private void RemoveTracker(ulong blockingId)
        {
            if (trackInputBlock == null){ return; }

            if (trackInputBlock.ContainsKey(blockingId))
            {
                trackInputBlock.Remove(blockingId);
            }
        }

        private void ClearTracker()
        {
            if (trackInputBlock == null){ return; }

            trackInputBlock.Clear();
        }

        public IReadOnlyDictionary<ulong, string> GetTrackContents()
        {
            return trackInputBlock ?? (trackInputBlock = new Dictionary<ulong, string>());
        }
    }
}

#endif