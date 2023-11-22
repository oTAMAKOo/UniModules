
#if !ENABLE_CRIWARE_ADX && !ENABLE_CRIWARE_ADX_LE

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.Sound
{
    public sealed class SoundParam
    {
        public float volume = 1f;
        public bool cancelIfPlaying = false;
    }

    public sealed class SoundManagement : SoundManagementBase<SoundManagement, SoundParam, SoundElement>
    {
        //----- params -----

        private static readonly Dictionary<SoundType, int> SoundLimitTable = new Dictionary<SoundType, int>
        {
            { SoundType.Bgm, 2 },
            { SoundType.Ambience, 4 },
            { SoundType.Jingle, 4 },
            { SoundType.Voice, 16 },
            { SoundType.Se, 32 },
        };

        //----- field -----

        private GameObject soundObject = null;
        
        private Dictionary<SoundType, GameObject> soundRoot = null;

        private Dictionary<SoundType, List<AudioSource>> audioSourceList = null;

        private Dictionary<SoundType, int> maxSoundLimit = null;

        private HashSet<Tuple<SoundType, AudioClip>> framePlayedAudioClips = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----
        
        private SoundManagement()
        {
            audioSourceList = new Dictionary<SoundType, List<AudioSource>>();
            soundRoot = new Dictionary<SoundType, GameObject>();
            maxSoundLimit = new Dictionary<SoundType, int>();
            framePlayedAudioClips = new HashSet<Tuple<SoundType, AudioClip>>();

            LogEnable = false;
        }

        public void Initialize(SoundParam defaultSoundParam)
        {
            if (initialized) { return; }

            soundObject = UnityUtility.CreateEmptyGameObject(null, "SoundManagement");

            var soundTypes = Enum.GetValues(typeof(SoundType)).Cast<SoundType>();

            foreach (var soundType in soundTypes)
            {
                var rootObject = UnityUtility.CreateEmptyGameObject(soundObject, soundType.ToString());

                soundRoot.Add(soundType, rootObject);
            }

            GameObject.DontDestroyOnLoad(soundObject);

            OnInitialize(defaultSoundParam);
            
            // 初期最大再生数.
            foreach (var item in SoundLimitTable)
            {
                SetSoundLimit(item.Key, item.Value);
            }

            // パラメータ更新通知.
            OnUpdateParamAsObservable()
                .Subscribe(x => ApplySoundParam(x))
                .AddTo(Disposable);

            // 一定周期でAudioSourceの更新を実行.
            Observable.Interval(TimeSpan.FromSeconds(5f))
                .Subscribe(_ => UpdateSoundSource())
                .AddTo(Disposable);

            // 同一フレーム再生リストクリア.
            Observable.EveryEndOfFrame()
                .Subscribe(_ => framePlayedAudioClips.Clear())
                .AddTo(Disposable);

            initialized = true;
        }

        /// <summary> 最大再生数設定 </summary>
        public void SetSoundLimit(SoundType type, int limit)
        {
            maxSoundLimit[type] = limit;

            UpdateSoundSource();
        }

        private void UpdateSoundSource()
        {
            var soundTypes = Enum.GetValues(typeof(SoundType)).Cast<SoundType>();

            foreach (var soundType in soundTypes)
            {
                var list = audioSourceList.GetValueOrDefault(soundType);

                if (list == null)
                {
                    list = new List<AudioSource>();

                    audioSourceList[soundType] = list;
                }

                var requireCount = maxSoundLimit.GetValueOrDefault(soundType);

                var root = soundRoot.GetValueOrDefault(soundType);

                // 足りない.
                while (list.Count < requireCount)
                {
                    var audioSource = UnityUtility.CreateGameObject<AudioSource>(root, SoundElement.EmptyName);

                    audioSource.transform.Reset();
                    audioSource.playOnAwake = false;

                    list.Add(audioSource);
                }

                // 多い.
                while (requireCount < list.Count)
                {
                    var target = GetUnuseAudioSource(soundType);

                    if (target == null){ break; }

                    list.Remove(target);
                }
            }
        }

        public IReadOnlyList<SoundElement> GetAllSounds(bool playing = true)
        {
            if (playing)
            {
                return soundElements.Where(x => x.IsPlaying).ToArray();
            }

            return soundElements;
        }

        /// <summary> 再生設定を適用 </summary>
        private void ApplySoundParam(SoundType? type = null)
        {
            var param = type != null ? GetSoundParam(type.Value) : defaultSoundParam;

            if (param != null)
            {
                foreach (var element in soundElements)
                {
                    SetVolume(element, param.volume);
                }
            }
        }

        /// <summary> サウンドを再生 </summary>
        public SoundElement Play(SoundType type, AudioClip clip)
        {
            SoundElement element = null;

            var currentNum = soundElements.Count(x => x.Type == type);
            var limitNum = maxSoundLimit.GetValueOrDefault(type);

            var soundParam = GetSoundParam(type);

            for (var i = limitNum; i < currentNum; i++)
            {
                var remove = soundElements.Where(x => x.Type == type).FindMin(x => x.Number);

                Stop(remove);
            }

            var playingElement = FindPlayingElement(type, clip);

            // 同一フレームで同じ音は再生しない.

            if (framePlayedAudioClips.Contains(Tuple.Create(type, clip)))
            {
                element = playingElement;

                if (element != null)
                {
                    return element;
                }
            }

            // パラメータで再生中キャンセルが有効.

            if (soundParam != null && soundParam.cancelIfPlaying)
            {
                element = playingElement;

                if (element != null)
                {
                    return element;
                }
            }

            var audioSource = GetUnuseAudioSource(type);

            if (audioSource == null){ return null; }

            element = new SoundElement(type, audioSource, clip);

            if (soundParam != null)
            {
                element.Volume = soundParam.volume;
            }

            element.Source.Play();

            element.Update();

            element.OnFinishAsObservable()
                .Subscribe(_ => soundElements.Remove(element))
                .AddTo(Disposable);
            
            soundElements.Add(element);

            framePlayedAudioClips.Add(Tuple.Create(type, clip));

            if (onPlay != null)
            {
                onPlay.OnNext(element);
            }

            if (LogEnable && UnityConsole.Enable)
            {
                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Play : {clip.name}");
            }

            return element;
        }

        /// <summary> サウンド中断 </summary>
        public void Pause(SoundElement element)
        {
            if (element == null){ return; }

            if (element.IsPause){ return; }
            
            element.Source.Pause();

            element.Update();

            if (onPause != null)
            {
                onPause.OnNext(element);
            }
        }

        /// <summary> 全サウンド中断 </summary>
        public void PauseAll()
        {
            foreach (var element in soundElements)
            {
                Pause(element);
            }
        }

        /// <summary> サウンド復帰 </summary>
        public void Resume(SoundElement element)
        {
            if (element == null){ return; }

            if (!element.IsPause){ return; }

            element.Source.UnPause();

            element.Update();

            if (onResume != null)
            {
                onResume.OnNext(element);
            }
        }

        /// <summary> 全サウンド復帰 </summary>
        public void ResumeAll()
        {
            foreach (var element in soundElements)
            {
                Resume(element);
            }
        }

        /// <summary> サウンド停止 </summary>
        public void Stop(SoundElement element)
        {
            if (element == null){ return; }
            
            element.Source.Stop();

            element.Update();

            if (onStop != null)
            {
                onStop.OnNext(element);
            }

            if (soundElements.Contains(element))
            {
                soundElements.Remove(element);
            }
        }

        /// <summary> 全サウンドを停止 </summary>
        public void StopAll()
        {
            foreach (var element in soundElements)
            {
                Stop(element);
            }
        }

        /// <summary> 個別に音量変更. </summary>
        public void SetVolume(SoundElement element, float value)
        {
            element.Source.volume = value;
        }

        public void ReleaseAll(bool force = false)
        {
            if (force)
            {
                foreach (var element in soundElements)
                {
                    if (onRelease != null)
                    {
                        onRelease.OnNext(element);
                    }
                }

                soundElements.Clear();
            }
            else
            {
                var releaseList = soundElements.Where(x => !x.IsPlaying).ToArray();

                foreach (var element in releaseList)
                {
                    if (onRelease != null)
                    {
                        onRelease.OnNext(element);
                    }

                    soundElements.Remove(element);
                }
            }

            foreach (var item in audioSourceList)
            {
                foreach (var audioSource in item.Value)
                {
                    if (soundElements.Any(x => x.Source == audioSource)){ continue; }

                    audioSource.clip = null;
                    audioSource.transform.Reset();
                }
            }
        }

        public async UniTask FadeIn(SoundElement element, float duration)
        {
            var tweener = DOTween.To(() => element.Source.volume, x => element.Source.volume = x, 1f, duration);

            await tweener.Play();
        }

        public async UniTask FadeOut(SoundElement element, float duration)
        {
            var tweener = DOTween.To(() => element.Source.volume, x => element.Source.volume = x, 0f, duration);

            await tweener.Play();
        }

        public async UniTask CrossFade(SoundElement inElement, SoundElement outElement, float duration)
        {
            if (inElement == null)
            {
                inElement.Source.volume = 0;

                if (!inElement.Source.isPlaying)
                {
                    inElement.Source.Play();
                }

                var soundParam = GetSoundParam(inElement.Type);

                var targetVolume = soundParam.volume;

                var firstVol = outElement.Source.volume;

                for (var time = 0f; time < duration; time += Time.deltaTime)
                {
                    var value = Mathf.Cos(time / duration * 2f);

                    inElement.Source.volume = (1 - value) / 2 * targetVolume;
                    outElement.Source.volume = (1 + value) / 2 * firstVol;

                    await UniTask.NextFrame();
                }

                inElement.Source.volume = targetVolume;

                outElement.Source.volume = 0f;
            }
            else
            {
                await FadeOut(outElement, duration);
            }

            Stop(outElement);
        }

        /// <summary> 対象のサウンドが再生中か. </summary>
        private SoundElement FindPlayingElement(Enum type, AudioClip clip)
        {
            // 再生中 + 同カテゴリ + 同アセットのサウンドを検索.
            var element = soundElements.FirstOrDefault(x =>
            {
                return x.IsPlaying && x.Type.CompareTo(type) == 0 && x.Clip == clip;
            });

            return element;
        }

        private AudioSource GetUnuseAudioSource(SoundType soundType)
        {
            var list = audioSourceList.GetValueOrDefault(soundType);

            if (list == null){ return null; }

            return list.FirstOrDefault(x => soundElements.All(y => y.Source != x) && !x.isPlaying);
        }
    }
}

#endif