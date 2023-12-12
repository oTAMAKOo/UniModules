
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Sound
{
    public interface ISoundElement
    {
        void Update();
    }

    public abstract class SoundManagementCore<TInstance, TSoundParam, TSoundElement> : Singleton<TInstance> 
        where TInstance : SoundManagementCore<TInstance, TSoundParam, TSoundElement>
        where TSoundParam : class, new()
        where TSoundElement : ISoundElement
    {
        //----- params -----

        public static readonly string ConsoleEventName = "Sound";
        public static readonly Color ConsoleEventColor = new Color(0.85f, 0.45f, 0.85f);

        //----- field -----

        private float masterVolume = 1.0f;

        protected List<TSoundElement> soundElements = null;

        protected TSoundParam defaultSoundParam = null;
        protected Dictionary<SoundType, TSoundParam> soundParams = null;

        
        private Subject<float> onUpdateMasterVolume = null;

        // サウンド設定更新通知.
        private Subject<SoundType> onUpdateParam = null;

        // サウンド通知.
        protected Subject<TSoundElement> onPlay = null;
        protected Subject<TSoundElement> onStop = null;
        protected Subject<TSoundElement> onPause = null;
        protected Subject<TSoundElement> onResume = null;
        protected Subject<TSoundElement> onRelease = null;

        //----- property -----

        public bool LogEnable { get; set; }

        public float Volume
        {
            get { return masterVolume; }

            set
            {
                masterVolume = Math.Clamp(value, 0f, 1f);

                if (onUpdateMasterVolume != null)
                {
                    onUpdateMasterVolume.OnNext(masterVolume);
                }
            }
        }

        //----- method -----

        protected void OnInitialize(TSoundParam defaultSoundParam)
        {
            this.defaultSoundParam = defaultSoundParam;

            soundParams = new Dictionary<SoundType, TSoundParam>();
            soundElements = new List<TSoundElement>();

            // サウンドの状態更新.
            Observable.EveryEndOfFrame()
                .Subscribe(_ => UpdateElement())
                .AddTo(Disposable);
        }

        protected void UpdateElement()
        {
            // 呼ばれる頻度が多いのでforeachを使わない.
            for (var i = 0; i < soundElements.Count; ++i)
            {
                soundElements[i].Update();
            }
        }

        /// <summary> 再生設定を登録 </summary>
        public void RegisterSoundType(SoundType type, TSoundParam param)
        {
            soundParams[type] = param;

            if (onUpdateParam != null)
            {
                onUpdateParam.OnNext(type);
            }
        }

        /// <summary> 再生設定を削除 </summary>
        public void RemoveSoundType(SoundType type)
        {
            if (soundParams.ContainsKey(type))
            {
                soundParams.Remove(type);
            }

            if (onUpdateParam != null)
            {
                onUpdateParam.OnNext(type);
            }
        }

        /// <summary> 再生設定を更新 </summary>
        public void UpdateSoundParam(SoundType type)
        {
            if (onUpdateParam != null)
            {
                onUpdateParam.OnNext(type);
            }
        }

        /// <summary> 再生設定を取得 </summary>
        public TSoundParam GetSoundParam(SoundType type)
        {
            return soundParams.GetValueOrDefault(type);
        }

        /// <summary> マスターボリューム変更通知 </summary>
        public IObservable<float> OnUpdateMasterVolume()
        {
            return onUpdateMasterVolume ?? (onUpdateMasterVolume = new Subject<float>());
        }

        /// <summary> サウンド設定更新通知 </summary>
        public IObservable<SoundType> OnUpdateParamAsObservable()
        {
            return onUpdateParam ?? (onUpdateParam = new Subject<SoundType>());
        }

        /// <summary> 再生通知 </summary>
        public IObservable<TSoundElement> OnPlayAsObservable()
        {
            return onPlay ?? (onPlay = new Subject<TSoundElement>());
        }

        /// <summary> 停止通知 </summary>
        public IObservable<TSoundElement> OnStopAsObservable()
        {
            return onStop ?? (onStop = new Subject<TSoundElement>());
        }

        /// <summary> 中断通知 </summary>
        public IObservable<TSoundElement> OnPauseAsObservable()
        {
            return onPause ?? (onPause = new Subject<TSoundElement>());
        }

        /// <summary> 復帰通知 </summary>
        public IObservable<TSoundElement> OnResumeAsObservable()
        {
            return onResume ?? (onResume = new Subject<TSoundElement>());
        }

        /// <summary> 解放通知 </summary>
        public IObservable<TSoundElement> OnReleaseAsObservable()
        {
            return onRelease ?? (onRelease = new Subject<TSoundElement>());
        }
    }
}