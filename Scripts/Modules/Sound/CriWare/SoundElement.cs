
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

using UnityEngine;
using System;
using CriWare;
using UniRx;

namespace Modules.Sound
{
    public sealed class SoundElement : ISoundElement
    {
        //----- params -----

        //----- field -----

        private CriAtomExPlayback playback;
        
        private Subject<CriAtomExSequencer.CriAtomExSequenceEventInfo> onSoundEvent = null;

        private Subject<Unit> onFinish = null;

        //----- property -----

        public SoundType Type { get; private set; }

        public CueInfo CueInfo { get; private set; }
        
        public SoundSheet SoundSheet { get; private set; }

        public float Volume { get; private set; }

        public float? FinishTime { get; private set; }

        public bool IsPaused { get { return playback.IsPaused(); } }

        public bool IsPlaying { get { return !FinishTime.HasValue; } }

        //----- method -----

        public SoundElement(SoundType type, SoundSheet soundSheet, CueInfo cueInfo, CriAtomExPlayback playback, float volume)
        {
            this.playback = playback;

            Type = type;
            SoundSheet = soundSheet;
            CueInfo = cueInfo;
            Volume = volume;
        }

        public CriAtomExPlayback GetPlayback()
        {
            return playback;
        }

        public void Update()
        {
            if (FinishTime.HasValue) { return; }

            if (!CriAtomPlugin.isInitialized) { return; }

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

        public void Stop(bool ignoresReleaseTime = false)
        {
            var soundManagement = SoundManagement.Instance;

            soundManagement.Stop(this, ignoresReleaseTime);
        }

        public void Pause()
        {
            var soundManagement = SoundManagement.Instance;

            soundManagement.Pause(this);
        }

        public void Resume(CriAtomEx.ResumeMode resumeMode = CriAtomEx.ResumeMode.AllPlayback)
        {
            var soundManagement = SoundManagement.Instance;

            soundManagement.Resume(this, resumeMode);
        }

        public void InvokeSoundEvent(CriAtomExSequencer.CriAtomExSequenceEventInfo eventInfo)
        {
            if (onSoundEvent != null)
            {
                onSoundEvent.OnNext(eventInfo);
            }
        }

        public void SetVolume(float volume)
        {
            var soundManagement = SoundManagement.Instance;

            Volume = volume;

            soundManagement.SetVolume(this, Volume);
        }

        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }

        /// <summary> サウンドに埋め込まれたイベント通知 </summary>
        public IObservable<CriAtomExSequencer.CriAtomExSequenceEventInfo> OnSoundEventAsObservable()
        {
            return onSoundEvent ?? (onSoundEvent = new Subject<CriAtomExSequencer.CriAtomExSequenceEventInfo>());
        }
    }
}

#endif
