﻿
#if ENABLE_CRIWARE_ADX

namespace Modules.Sound
{
    public sealed class CueInfo
    {
        public string CueSheet { get; private set; }
        public string Cue { get; private set; }
        public string FilePath { get; private set; }

        public CueInfo(string filePath, string cueSheetPath, string cue)
        {
            FilePath = filePath;
            CueSheet = cueSheetPath;
            Cue = cue;
        }
    }
}

#endif
