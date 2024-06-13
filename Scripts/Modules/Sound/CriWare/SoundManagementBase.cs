
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CriWare;
using UniRx;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Sound
{
    public sealed class SoundParam
    {
        public float volume = 1f;
        public bool cancelIfPlaying = false;
    }

    public interface ISoundManagement
    {
        void Pause(SoundElement element);

        void Resume(SoundElement element, CriAtomEx.ResumeMode resumeMode = CriAtomEx.ResumeMode.AllPlayback);

        void Stop(SoundElement element, bool ignoresReleaseTime = false);

        void SetVolume(SoundElement element, float volume);
    }

    public abstract partial class SoundManagementBase<TInstance, TSound> : 
        SoundManagementCore<TInstance, SoundParam, SoundElement>, ISoundManagement
        where TInstance : SoundManagementBase<TInstance, TSound>
    {
        //----- params -----

        private const float DefaultReleaseTime = 30f;

        //----- field -----

        private CriAtomExPlayer mainPlayer = null;

        private Dictionary<string, SoundSheet> managedSoundSheets = null;

        private HashSet<Tuple<string, string>> currentFramePlayedSounds = null;

        private Subject<Unit> onPauseAll = null;
        private Subject<CriAtomEx.ResumeMode> onResumeAll = null;

        private bool initialized = false;

        //----- property -----

        public CriAtomExPlayer Player { get { return mainPlayer; } }

        public float ReleaseTime { get; set; }

        //----- method -----

        protected SoundManagementBase()
        {
            ReleaseTime = DefaultReleaseTime;

            managedSoundSheets = new Dictionary<string, SoundSheet>();
            currentFramePlayedSounds = new HashSet<Tuple<string, string>>();
        }

        public void Initialize(SoundParam defaultSoundParam)
        {
            if (initialized) { return; }

            mainPlayer = new CriAtomExPlayer();

            OnInitialize(defaultSoundParam);

            // デフォルトのサウンド設定を適用.
            SetDefaultSoundParam(mainPlayer);

            // サウンドイベントを受信.

            CriAtomExSequencer.OnCallback += ReceiveSoundEvent;

            // 一定周期で未使用状態になったAcbの解放を行う.
            Observable.Interval(TimeSpan.FromSeconds(5f))
                .Subscribe(_ => ReleaseSoundSheet())
                .AddTo(Disposable);

            // 毎フレームの最後に再生済みリストをクリア.
            Observable.EveryEndOfFrame()
                .Subscribe(_ => currentFramePlayedSounds.Clear())
                .AddTo(Disposable);

            // パラメータ更新通知.
            OnUpdateParamAsObservable()
                .Subscribe(x =>
                    {
                        ApplyVolume(x);
                        ApplySoundParam(mainPlayer, x);
                    })
                .AddTo(Disposable);

            initialized = true;
        }

        public IReadOnlyList<SoundElement> GetAllSounds(bool playing = true)
        {
            if (soundElements == null){ return new SoundElement[0]; }

            if (playing)
            {
                return soundElements.Where(x => x.IsPlaying).ToArray();
            }

            return soundElements;
        }
        
        /// <summary> 内蔵アセットサウンドを再生. </summary>
        public SoundElement Play(SoundType type, TSound cue, float? volume = null)
        {
            var info = GetCueInfo(cue);

            return PlaySoundCore(mainPlayer, type, info, volume);
        }
        
        /// <summary> 外部アセットのサウンドを再生. </summary>
        public SoundElement Play(SoundType type, CueInfo info, float? volume = null)
        {
            return PlaySoundCore(mainPlayer, type, info, volume);
        }

        private SoundElement PlaySoundCore(CriAtomExPlayer player, SoundType type, CueInfo info, float? volume)
        {
            if (info == null) { return null; }

            var soundPlayKey = Tuple.Create(info.CueSheet, info.Cue);

            if (currentFramePlayedSounds.Contains(soundPlayKey))
            {
                return null;
            }

            var soundParam = GetSoundParam(type);

            var soundVolume = soundParam.volume;

            SoundElement element = null;

            if (soundParam != null)
            {
                soundVolume = soundParam.volume;

                if (soundParam.cancelIfPlaying)
                {
                    element = FindPlayingElement(type, info);

                    if (element != null)
                    {
                        return element;
                    }
                }
            }

            if (volume.HasValue)
            {
                soundVolume = volume.Value;
            }

            element = CreateSoundElement(player, info, type, soundVolume);

            if (element == null) { return null; }

            // 管理対象に追加.
            soundElements.Add(element);

            // 音量設定.

            SetVolume(element, soundVolume);

            // 再生.

            PlaySoundElement(element).Forget();

            currentFramePlayedSounds.Add(soundPlayKey); 

            if (onPlay != null)
            {
                onPlay.OnNext(element);
            }

            return element;
        } 

        /// <summary> サウンド中断 </summary>
        public void Pause(SoundElement element)
        {
            if (element == null) { return; }

            var playback = element.GetPlayback();

            playback.Pause();
            element.Update();

            if (onPause != null)
            {
                onPause.OnNext(element);
            }
        }

        /// <summary> 全サウンド中断 </summary>
        public void PauseAll()
        {
            if (soundElements.IsEmpty()) { return; }

            mainPlayer.Pause();

            foreach (var element in soundElements)
            {
                element.Update();

                if (onPause != null)
                {
                    onPause.OnNext(element);
                }
            }

            if (onPauseAll != null)
            {
                onPauseAll.OnNext(Unit.Default);
            }
        }

        /// <summary> サウンド復帰 </summary>
        public void Resume(SoundElement element, CriAtomEx.ResumeMode resumeMode = CriAtomEx.ResumeMode.AllPlayback)
        {
            if (element == null) { return; }

            var playback = element.GetPlayback();

            playback.Resume(resumeMode);
            element.Update();

            if (onResume != null)
            {
                onResume.OnNext(element);
            }
        }

        /// <summary> 全サウンド復帰 </summary>
        public void ResumeAll(CriAtomEx.ResumeMode resumeMode = CriAtomEx.ResumeMode.AllPlayback)
        {
            if (soundElements.IsEmpty()) { return; }

            mainPlayer.Resume(resumeMode);

            foreach (var element in soundElements)
            {
                element.Update();

                if (onResume != null)
                {
                    onResume.OnNext(element);
                }
            }

            if (onResumeAll != null)
            {
                onResumeAll.OnNext(resumeMode);
            }
        }

        /// <summary> サウンド停止 </summary>
        public void Stop(SoundElement element, bool ignoresReleaseTime = false)
        {
            if (element == null) { return; }

            var playback = element.GetPlayback();

            playback.Stop(ignoresReleaseTime);
            element.Update();

            if (onStop != null)
            {
                onStop.OnNext(element);
            }
        }

        /// <summary> 全サウンドを停止 </summary>
        public void StopAll(bool ignoresReleaseTime = false)
        {
            if (soundElements.IsEmpty()) { return; }

            foreach (var element in soundElements)
            {
                var playback = element.GetPlayback();

                playback.Stop(ignoresReleaseTime);
                element.Update();

                if (onStop != null)
                {
                    onStop.OnNext(element);
                }
            }

            soundElements.Clear();
        }

        /// <summary> 個別に音量変更. </summary>
        public void SetVolume(SoundElement element, float volume)
        {
            var player = element.GetPlayer();

            var soundParam = GetSoundParam(element.Type);

            var soundVolume = Volume * soundParam.volume * Mathf.Clamp01(volume);

            player.SetVolume(soundVolume);
            player.Update(element.GetPlayback());

            // デフォルトに戻す.
            SetDefaultSoundParam(player);
        }

        /// <summary> 対象のサウンドが再生中か. </summary>
        private SoundElement FindPlayingElement(Enum type, CueInfo info)
        {
            // シート取得.
            var soundSheet = GetSoundSheet(info);

            // 再生中 + 同カテゴリ + 同アセットのサウンドを検索.
            var element = soundElements.FirstOrDefault(x =>
            {
                if (!File.Exists(x.SoundSheet.AssetPath)) { return false; }

                return x.IsPlaying && x.Type.CompareTo(type) == 0 && x.SoundSheet.AssetPath == soundSheet.AssetPath;
            });

            return element;
        }

        /// <summary> 個別設定を戻す. </summary>
        private void ResetSoundParam(SoundElement element)
        {
            var player = element.GetPlayer();

            ApplySoundParam(player, element.Type);

            player.Update(element.GetPlayback());

            // デフォルトに戻す.
            SetDefaultSoundParam(player);
        }

        /// <summary> 再生中のサウンドに音量を反映. </summary>
        private void ApplyVolume(SoundType? type = null)
        {
            var targets = soundElements.Where(x => x.Type == type).ToArray();

            foreach (var target in targets)
            {
                SetVolume(target, target.Volume);
            }
        }

        /// <summary> CriAtomPlayerに再生設定を適用. </summary>
        private void ApplySoundParam(CriAtomExPlayer player, SoundType type)
        {
            var param = GetSoundParam(type);

            if (param != null)
            {
                player.SetVolume(param.volume);
            }
        }

        private void SetDefaultSoundParam(CriAtomExPlayer player)
        {
            player.SetVolume(defaultSoundParam.volume);
        }

        private SoundSheet GetSoundSheet(CueInfo cueInfo)
        {
            if (cueInfo == null) { return null; }

            var assetPath = cueInfo.FilePath;
            var soundSheet = managedSoundSheets.GetValueOrDefault(assetPath);

            if (soundSheet == null)
            {
                // パス情報生成.
                var acbPath = SoundSheet.AcbPath(assetPath);
                var awbPath = SoundSheet.AwbPath(assetPath);

                // ACBファイルのロード.
                CriAtomCueSheet cueSheet = null;

                try
                {
                    cueSheet = CriAtom.AddCueSheet(assetPath, acbPath, awbPath);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return null;
                }

                if (cueSheet.acb == null) { return null; }

                // ロードしたACBを保持した状態で再生成.
                soundSheet = new SoundSheet(assetPath, cueSheet.acb);

                managedSoundSheets.Add(soundSheet.AssetPath, soundSheet);
            }

            return soundSheet;
        }

        private SoundElement CreateSoundElement(CriAtomExPlayer player, CueInfo info, SoundType type, float volume)
        {
            // シート取得.
            var soundSheet = GetSoundSheet(info);

            if (soundSheet == null) { return null; }

            // 再生するキューの名前を指定.
            player.SetCue(soundSheet.Acb, info.Cue);

            // 再生パラメータ設定.
            ApplySoundParam(player, type);

            // 音量を設定.
            player.SetVolume(volume);

            // セットされた音声データの再生準備を開始.
            var playback = player.Prepare();

            // 再生失敗ならRemovedが返る.
            if (playback.GetStatus() == CriAtomExPlayback.Status.Removed) { return null; }

            // デフォルトに戻す.
            SetDefaultSoundParam(player);

            var element = new SoundElement(this, type, soundSheet, info, player, playback, volume);

            return element;
        }

        private async UniTask PlaySoundElement(SoundElement element)
        {
            var playback = element.GetPlayback();

            // 再生準備完了待ち.
            while (true)
            {
                if (!CriAtomPlugin.isInitialized){ return; }

                var status = playback.GetStatus();

                if (status == CriAtomExPlayback.Status.Playing){ break; }

                if (status == CriAtomExPlayback.Status.Removed)
                {
                    Stop(element);
                    
                    return;
                }

                await UniTask.NextFrame();
            }

            // ポーズを解除.
            playback.Resume(CriAtomEx.ResumeMode.PreparedPlayback);
        }

        private void ReleaseSoundSheet()
        {
            for (var i = 0; i < soundElements.Count; ++i)
            {
                var soundElement = soundElements[i];

                if (!soundElement.FinishTime.HasValue)
                {
                    continue;
                }

                // 終了確認した時間から一定時間経過していたら解放.
                if (soundElement.FinishTime.Value + ReleaseTime < Time.realtimeSinceStartup)
                {
                    soundElements.RemoveAt(i);

                    if (onRelease != null)
                    {
                        onRelease.OnNext(soundElement);
                    }
                }
            }

            var targets = new List<SoundSheet>();

            foreach (var item in managedSoundSheets)
            {
                var release = true;

                // 再生中のCueが存在したら生存.
                foreach (var soundElement in soundElements)
                {
                    if (item.Key == soundElement.SoundSheet.AssetPath)
                    {
                        release = false;
                        break;
                    }
                }

                if (release)
                {
                    targets.Add(item.Value);
                }
            }

            foreach (var target in targets)
            {
                CriAtom.RemoveCueSheet(target.AssetPath);
                managedSoundSheets.Remove(target.AssetPath);             
            }
        }

        public void ReleaseAll(bool force = false)
        {
            // 再生リストの情報.
            var releaseElements = new List<SoundElement>();

            foreach (var soundElement in soundElements)
            {
                if (!force && soundElement.IsPlaying) { continue; }

                releaseElements.Add(soundElement);
            }

            foreach (var item in releaseElements)
            {
                soundElements.Remove(item);

                if (onRelease != null)
                {
                    onRelease.OnNext(item);
                }
            }

            // 管理下の情報.
            var releaseList = new List<KeyValuePair<string, SoundSheet>>();

            foreach (var managedSoundSheet in managedSoundSheets)
            {
                // 再生中のCueは生存.
                if (soundElements.Any(x => managedSoundSheet.Key == x.SoundSheet.AssetPath)) { continue; }

                releaseList.Add(managedSoundSheet);
            }

            foreach (var item in releaseList)
            {
                CriAtom.RemoveCueSheet(item.Value.AssetPath);

                managedSoundSheets.Remove(item.Key);
            }
        }

        private void ReceiveSoundEvent(ref CriAtomExSequencer.CriAtomExSequenceEventInfo eventInfo)
        {
            foreach (var soundElement in soundElements)
            {
                if (!soundElement.IsPlaying){ continue; }

                var playback = soundElement.GetPlayback();

                if (playback.id != eventInfo.playbackId){ continue; }

                soundElement.InvokeSoundEvent(eventInfo);
            }
        }

        /// <summary> 全ポーズイベント </summary>
        public IObservable<Unit> OnPauseAllAsObservable()
        {
            return onPauseAll ?? (onPauseAll = new Subject<Unit>());
        }

        /// <summary> 全レジュームイベント </summary>
        public IObservable<CriAtomEx.ResumeMode> OnResumeAllAsObservable()
        {
            return onResumeAll ?? (onResumeAll = new Subject<CriAtomEx.ResumeMode>());
        }

        protected abstract CueInfo GetCueInfo(TSound cue);
    }
}

#endif

