
#if ENABLE_CRIWARE_ADX
﻿﻿
using UnityEngine;
using System.IO;

namespace Modules.SoundManagement
{
    public class CueInfo
    {
        public int CueId { get; private set; }
        public string CueSheet { get; private set; }
        public string Cue { get; private set; }
        public string CueSheetPath { get; private set; }
        public string Summary { get; private set; }

        public CueInfo(string cue, string path, string summary) : this(cue, path)
        {
            this.Summary = summary;
        }

        public CueInfo(string cue, string path)
        {
            this.Cue = cue;
            this.CueSheetPath = path;

            this.CueSheet = Path.GetFileNameWithoutExtension(path);
            this.CueId = CueSheetPath.GetHashCode();
        }
    }
}

#endif
