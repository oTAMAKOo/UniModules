
#if ENABLE_CRIWARE_ADX
﻿﻿
using UnityEngine;
using System.IO;

namespace Modules.Sound
{
    public sealed class CueInfo
    {
        public int CueId { get; private set; }
        public string CueSheet { get; private set; }
        public string Cue { get; private set; }
        public string FilePath { get; private set; }
        public string Summary { get; private set; }

        public CueInfo(string filePath, string cueSheetPath, string cue)
        {
            FilePath = filePath;
            CueSheet = cueSheetPath;
            Cue = cue;

            CueId = string.Format("{0}-{1}", FilePath, Cue).GetHashCode();
        }

        public CueInfo(string filePath, string cueSheetPath, string cue, string summary) : this(filePath, cueSheetPath, cue)
        {
            Summary = summary;
        }
    }
}

#endif
