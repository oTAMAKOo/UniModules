
#if !ENABLE_CRIWARE_ADX && !ENABLE_CRIWARE_ADX_LE

using UnityEngine;
using System;
using Extensions;
using UniRx;
using Constants;

namespace Modules.Sound
{
    public class SoundElement : ISoundElement
    {
        //----- params -----

        public const string EmptyName = "empty";

        //----- field -----

        private float volume = 1f;

        private Subject<Unit> onFinish = null;
        
        private static uint nextNumber = 0;

        //----- property -----

        public uint Number { get; private set; }

        public SoundType Type { get; private set; }

        public AudioSource Source { get; private set; }

        public AudioClip Clip { get; private set; }

        public float? FinishTime { get; protected set; }

        public virtual bool IsPlaying { get; protected set; }

        public virtual bool IsPause { get; protected set; }

        public virtual float Volume
        {
            get { return volume; }
            set
            {
                volume = value;

                UpdateVolume();
            }
        }

        //----- method -----

        public SoundElement(SoundType type, AudioSource source, AudioClip clip)
        {
            Number = nextNumber++;

            Type = type;
            Source = source;
            Clip = clip;

            if (Source != null)
            {
                Source.transform.name = $"[{type}] {clip.name}";
                Source.transform.Reset();
                Source.clip = Clip;
            }
        }

        public virtual void Play()
        {
            if (Source == null){ return; }

            Source.Play();
        }

        public virtual void Stop()
        {
            if (Source == null){ return; }

            Source.Stop();
        }

        public virtual void Pause()
        {
            if (Source == null){ return; }

            Source.Pause();
        }

        public virtual void UnPause()
        {
            if (Source == null){ return; }

            Source.UnPause();
        }

        public virtual void Update()
        {
            if (FinishTime.HasValue) { return; }

            IsPlaying = Source.isPlaying;

            IsPause = !Source.isPlaying && Source.time != 0;
            
            if (!IsPlaying && !IsPause)
            {
                FinishTime = Time.realtimeSinceStartup;

                IsPlaying = false;

                Source.clip = null;
                Source.transform.name = EmptyName;
                Source.transform.Reset();

                if (onFinish != null)
                {
                    onFinish.OnNext(Unit.Default);
                }
            }
        }

        public void UpdateVolume()
        {
            var soundManagement = SoundManagement.Instance;

            var soundParam = soundManagement.GetSoundParam(Type);

            Source.volume = soundManagement.Volume * soundParam.volume *Volume;
        }

        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }
    }
}

#endif