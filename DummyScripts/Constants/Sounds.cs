
#if UNIMODULE_DUMMY

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

using System;
using System.Linq;
using System.Collections.Generic;
using CriWare;
using Extensions;

namespace Modules.Sound
{
    public static partial class Sounds
    {
        private static Dictionary<Cue, CueInfo> cache = new Dictionary<Cue,CueInfo>();

        public enum Cue
        {

        }

        private static Dictionary<Cue, Tuple<string, string>> internalSounds = new Dictionary<Cue, Tuple<string, string>>()
        {

        };

        public static CueInfo[] GetInternalFileInfo()
        {
            return internalSounds
                .Select(x => GetCueInfo(x.Key))
                .DistinctBy(x => x.CueSheet)
                .ToArray();
        }

        public static CueInfo GetCueInfo(Cue cue)
        {
            var cueInfo = cache.GetValueOrDefault(cue);

            if (cueInfo == null)
            {
                var fileDirectory = string.Empty;

                #if UNITY_EDITOR

                fileDirectory = UnityPathUtility.StreamingAssetsPath;

                #else

                fileDirectory = Common.streamingAssetsPath;

                #endif

                var info = internalSounds.GetValueOrDefault(cue);

                var filePath = PathUtility.Combine(fileDirectory, info.Item1);

                cueInfo = new CueInfo(filePath, info.Item1, info.Item2);

                cache.Add(cue, cueInfo);
            }

            return cueInfo;
        }
    }
}

#endif

#endif
