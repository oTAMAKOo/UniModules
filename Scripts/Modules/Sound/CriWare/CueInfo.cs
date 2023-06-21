
#if ENABLE_CRIWARE_ADX

namespace Modules.Sound
{
    public sealed class CueInfo
    {
        public int CueId { get; private set; }
        public string CueSheet { get; private set; }
        public string Cue { get; private set; }
        public string FilePath { get; private set; }

        public CueInfo(string filePath, string cueSheetPath, string cue)
        {
            var soundManagement = SoundManagement.Instance;

            FilePath = filePath;
            CueSheet = cueSheetPath;
            Cue = cue;

            CueId = soundManagement.GetCueId(filePath, cue);
        }
    }
}

#endif
