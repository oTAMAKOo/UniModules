
#if !ENABLE_CRIWARE_ADX && !ENABLE_CRIWARE_ADX_LE

using UnityEngine;
using System;
using Extensions;
using UniRx;

namespace Modules.Sound
{
    public sealed class SoundElement : ISoundElement
    {
        //----- params -----

        public const string EmptyName = "empty";

        //----- field -----

        private Subject<Unit> onFinish = null;
        
        private static uint nextNumber = 0;

        //----- property -----

        public uint Number { get; private set; }

        public SoundType Type { get; private set; }

        public AudioSource Source { get; private set; }

        public AudioClip Clip { get; private set; }

        public bool IsPlaying { get; private set; }

        public bool IsPause { get; private set; }

        public float? FinishTime { get; private set; }

        public float Volume
        {
            get { return Source.volume; }
            set { Source.volume = value; }
        }

        //----- method -----

        public SoundElement(SoundType type, AudioSource source, AudioClip clip)
        {
            Number = nextNumber++;

            Type = type;
            Source = source;
            Clip = clip;
            
            Source.transform.name = $"[{type}] {clip.name}";
            Source.transform.Reset();
            Source.clip = Clip;
        }

        public void Update()
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

        public IObservable<Unit> OnFinishAsObservable()
        {
            return onFinish ?? (onFinish = new Subject<Unit>());
        }
    }
}

#endif