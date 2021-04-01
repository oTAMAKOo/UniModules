
#if ENABLE_CRIWARE_ADX

using UnityEngine;
using System;
using CriWare;
using UniRx;

namespace Modules.SoundManagement
{
    public sealed class SoundElement
    {
        //----- params -----

        //----- field -----

        private CriAtomExPlayback playback;
        private Subject<Unit> onFinish = null;

        //----- property -----

        public SoundType Type { get; private set; }
        public CueInfo CueInfo { get; private set; }
        public SoundSheet SoundSheet { get; private set; }
        public float? FinishTime { get; private set; }

        public bool IsPlaying { get { return !FinishTime.HasValue; } }

        //----- method -----

        public SoundElement(SoundType type, SoundSheet soundSheet, CueInfo cueInfo, CriAtomExPlayback playback)
        {
            this.playback = playback;

            this.Type = type;
            this.SoundSheet = soundSheet;
            this.CueInfo = cueInfo;
        }

        public CriAtomExPlayback GetPlayback()
        {
            return playback;
        }

        public void SetVolume(float value)
        {
            SoundManagement.SetVolume(this, value);
        }

        public void Update()
        {
            if (FinishTime.HasValue) { return; }

            // 終了時間を記録.
            if (playback.GetStatus() == CriAtomExPlayback.Status.Removed)
            {
                FinishTime = Time.realtimeSinceStartup;

                if (onFinish != null)
                {
                    onFinish.OnNext(Unit.Default);
                }
            }
        }

        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }
    }
}

#endif
