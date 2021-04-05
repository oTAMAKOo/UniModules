
#if ENABLE_CRIWARE_ADX

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CriWare;
using UniRx;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.SoundManagement
{
    public enum SoundType
    {
        /// <summary>BGM</summary>
        Bgm,

        /// <summary>ジングル</summary>
        Jingle,

        /// <summary>ボイス</summary>
        Voice,

        /// <summary>SE</summary>
        Se,

        /// <summary>環境音</summary>
        Ambience,
    }

    public sealed class SoundParam
    {
        public float volume = 1f;
        public bool cancelIfPlaying = false;
    }

    public sealed class SoundEventParam
    {
        /// <summary>イベント位置 (sec) </summary>
        public int position;

        /// <summary>イベントID </summary>
        public int eventId;

        /// <summary>イベントタグ文字列 </summary>
        public string tag;
    }

    public sealed class SoundManagement : Singleton<SoundManagement>
    {
        //----- params -----

        private const float DefaultReleaseTime = 30f;

        private const char SoundEventSeparator = '\t';

        public static readonly string ConsoleEventName = "Sound";
        public static readonly Color ConsoleEventColor = new Color(0.85f, 0.45f, 0.85f);

        //----- field -----

        private CriAtomExPlayer player = null;

        private Dictionary<string, SoundSheet> managedSoundSheets = null;
        private List<SoundElement> soundElements = null;

        private SoundParam defaultSoundParam = null;
        private Dictionary<SoundType, SoundParam> soundParams = null;

        // サウンド通知.
        private Subject<SoundElement> onPlay = null;
        private Subject<SoundElement> onStop = null;
        private Subject<SoundElement> onPause = null;
        private Subject<SoundElement> onResume = null;

        // サウンドイベント.
        private Subject<SoundEventParam> onSoundEvent = null;

        private bool initialized = false;

        //----- property -----

        public bool LogEnable { get; set; }

        public float ReleaseTime { get; set; }

        //----- method -----

        private SoundManagement()
        {
            ReleaseTime = DefaultReleaseTime;
            LogEnable = false;
        }

        public void Initialize(SoundParam defaultSoundParam)
        {
            if (initialized) { return; }

            this.defaultSoundParam = defaultSoundParam;

            player = new CriAtomExPlayer();

            soundParams = new Dictionary<SoundType, SoundParam>();
            managedSoundSheets = new Dictionary<string, SoundSheet>();
            soundElements = new List<SoundElement>();

            // デフォルトのサウンド設定を適用.
            SetSoundParam();

            // サウンドイベントを受信.
            CriAtomExSequencer.SetEventCallback(SoundEventCallback, SoundEventSeparator.ToString());

            // サウンドの状態更新.
            Observable.EveryEndOfFrame()
                .Subscribe(_ => UpdateElement())
                .AddTo(Disposable); ;

            // 一定周期で未使用状態になったAcbの解放を行う.
            Observable.Interval(TimeSpan.FromSeconds(5f))
                .Subscribe(_ => ReleaseSoundSheet())
                .AddTo(Disposable);

            initialized = true;
        }

        public IReadOnlyList<SoundElement> GetAllSounds(bool playing = true)
        {
            if (playing)
            {
                return soundElements.Where(x => x.IsPlaying).ToArray();
            }

            return soundElements;
        }

        /// <summary>
        /// 再生設定を登録.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="param"></param>
        public void RegisterSoundType(SoundType type, SoundParam param)
        {
            soundParams[type] = param;

            // 再生中の音量に適用.
            foreach (var soundElement in soundElements)
            {
                if (soundElement.Type == type)
                {
                    SetVolume(soundElement, soundParams[type].volume);
                }
            }
        }

        /// <summary>
        /// 再生設定を抹消.
        /// </summary>
        /// <param name="type"></param>
        public void RemoveSoundType(SoundType type)
        {
            if (soundParams.ContainsKey(type))
            {
                soundParams.Remove(type);
            }
        }

        /// <summary>
        /// 再生設定を取得.
        /// </summary>
        /// <param name="type"></param>
        public SoundParam GetSoundParam(SoundType type)
        {
            var param = soundParams.GetValueOrDefault(type);

            if (param == null)
            {
                // エラーが大量に発生しないように空のパラメータを追加.
                soundParams[type] = new SoundParam();

                param = soundParams[type];

                Debug.LogErrorFormat("未登録の再生属性が指定されました. ({0})", type);
            }

            return param;
        }

        /// <summary>
        /// InternalResources内のサウンドを再生.
        /// </summary>
        public static SoundElement Play(SoundType type, Sounds.Cue cue)
        {
            var soundParam = Instance.GetSoundParam(type);
            var info = Sounds.GetCueInfo(cue);

            if (soundParam.cancelIfPlaying)
            {
                var element = FindPlayingElement(type, info);

                if (element != null)
                {
                    return element;
                }
            }

            return info != null ? Play(type, info) : null;
        }

        /// <summary>
        ///  ExternalResources内のサウンドを再生.
        /// </summary>
        public static SoundElement Play(SoundType type, CueInfo info)
        {
            if (info == null) { return null; }

            var soundParam = Instance.GetSoundParam(type);

            SoundElement element = null;

            if (soundParam.cancelIfPlaying)
            {
                element = FindPlayingElement(type, info);

                if (element != null)
                {
                    return element;
                }
            }

            element = Instance.GetSoundElement(info, type);

            if (element == null) { return null; }

            // 管理対象に追加.
            Instance.soundElements.Add(element);

            // 音量設定.
            SetVolume(element, soundParam.volume);

            Observable.FromCoroutine(() => Instance.PlaySoundElement(element)).Subscribe().AddTo(Instance.Disposable);

            if (Instance.onPlay != null)
            {
                Instance.onPlay.OnNext(element);
            }

            return element;
        }

        /// <summary> サウンド中断 </summary>
        public static void Pause(SoundElement element)
        {
            if (element == null) { return; }

            var playback = element.GetPlayback();

            playback.Pause();
            element.Update();

            if (Instance.onPause != null)
            {
                Instance.onPause.OnNext(element);
            }
        }

        /// <summary> 全サウンド中断 </summary>
        public static void PauseAll()
        {
            if (Instance.soundElements.IsEmpty()) { return; }

            Instance.player.Pause();

            foreach (var element in Instance.soundElements)
            {
                element.Update();

                if (Instance.onPause != null)
                {
                    Instance.onPause.OnNext(element);
                }
            }
        }

        /// <summary> サウンド復帰 </summary>
        public static void Resume(SoundElement element, CriAtomEx.ResumeMode resumeMode = CriAtomEx.ResumeMode.AllPlayback)
        {
            if (element == null) { return; }

            var playback = element.GetPlayback();

            playback.Resume(resumeMode);
            element.Update();

            if (Instance.onResume != null)
            {
                Instance.onResume.OnNext(element);
            }
        }

        /// <summary> 全サウンド復帰 </summary>
        public static void ResumeAll(CriAtomEx.ResumeMode resumeMode = CriAtomEx.ResumeMode.AllPlayback)
        {
            if (Instance.soundElements.IsEmpty()) { return; }

            Instance.player.Resume(resumeMode);

            foreach (var element in Instance.soundElements)
            {
                element.Update();

                if (Instance.onResume != null)
                {
                    Instance.onResume.OnNext(element);
                }
            }
        }

        /// <summary> サウンド停止 </summary>
        public static void Stop(SoundElement element, bool ignoresReleaseTime = false)
        {
            if (element == null) { return; }

            var playback = element.GetPlayback();

            playback.Stop(ignoresReleaseTime);
            element.Update();

            if (Instance.onStop != null)
            {
                Instance.onStop.OnNext(element);
            }
        }

        /// <summary> 全サウンドを停止 </summary>
        public static void StopAll(bool ignoresReleaseTime = false)
        {
            if (Instance.soundElements.IsEmpty()) { return; }

            foreach (var element in Instance.soundElements)
            {
                var playback = element.GetPlayback();

                playback.Stop(ignoresReleaseTime);
                element.Update();

                if (Instance.onStop != null)
                {
                    Instance.onStop.OnNext(element);
                }
            }

            Instance.soundElements.Clear();
        }

        /// <summary>
        /// 個別に音量変更.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetVolume(SoundElement element, float value)
        {
            Instance.player.SetVolume(value);
            Instance.player.Update(element.GetPlayback());

            // デフォルトに戻す.
            Instance.SetSoundParam();
        }

        /// <summary>
        /// 対象のサウンドが再生中か.
        /// </summary>
        private static SoundElement FindPlayingElement(Enum type, CueInfo info)
        {
            // シート取得.
            var soundSheet = Instance.GetSoundSheet(info);

            // 再生中 + 同カテゴリ + 同アセットのサウンドを検索.
            var element = Instance.soundElements.FirstOrDefault(x =>
            {
                if (!File.Exists(x.SoundSheet.AssetPath)) { return false; }

                return x.IsPlaying && x.Type.CompareTo(type) == 0 && x.SoundSheet.AssetPath == soundSheet.AssetPath;
            });

            return element;
        }

        /// <summary>
        /// 個別設定を戻す.
        /// </summary>
        private static void ResetSoundParam(SoundElement element)
        {
            Instance.SetSoundParam(element.Type);

            Instance.player.Update(element.GetPlayback());

            // デフォルトに戻す.
            Instance.SetSoundParam();
        }

        /// <summary>
        /// CriAtomPlayerに再生設定を適用.
        /// </summary>
        private void SetSoundParam(SoundType? type = null)
        {
            var param = type != null ? GetSoundParam(type.Value) : defaultSoundParam;

            player.SetVolume(param.volume);
        }

        private SoundSheet GetSoundSheet(CueInfo cueInfo)
        {
            if (cueInfo == null) { return null; }

            var assetPath = cueInfo.CueSheetPath;
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


                if (LogEnable && Debug.isDebugBuild)
                {
                    var builder = new StringBuilder();

                    builder.AppendFormat("Load : {0} : {1}", cueInfo.Cue, cueInfo.CueId).AppendLine();
                    builder.AppendLine();
                    builder.AppendFormat("Cue : {0}", cueInfo.Cue).AppendLine();
                    builder.AppendFormat("CueId : {0}", cueInfo.CueId).AppendLine();
                    builder.AppendFormat("FileName : {0}", Path.GetFileName(acbPath)).AppendLine();

                    if (!string.IsNullOrEmpty(cueInfo.Summary))
                    {
                        builder.AppendFormat("Summary: {0}", cueInfo.Summary).AppendLine();
                    }

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                }
            }

            return soundSheet;
        }

        private SoundElement GetSoundElement(CueInfo info, SoundType type)
        {
            // シート取得.
            var soundSheet = GetSoundSheet(info);

            if (soundSheet == null) { return null; }

            // 再生するキューの名前を指定.
            player.SetCue(soundSheet.Acb, info.Cue);

            // 再生パラメータ設定.
            SetSoundParam(type);

            // セットされた音声データの再生準備を開始.
            var playback = player.Prepare();

            // 再生失敗ならRemovedが返る.
            if (playback.GetStatus() == CriAtomExPlayback.Status.Removed) { return null; }

            // デフォルトに戻す.
            SetSoundParam();

            var element = new SoundElement(type, soundSheet, info, playback);

            return element;
        }

        private IEnumerator PlaySoundElement(SoundElement element)
        {
            var playback = element.GetPlayback();

            // 再生準備完了待ち.
            while (playback.GetStatus() != CriAtomExPlayback.Status.Playing)
            {
                yield return null;
            }

            // ポーズを解除.
            playback.Resume(CriAtomEx.ResumeMode.PreparedPlayback);
        }

        private void UpdateElement()
        {
            // 呼ばれる頻度が多いのでforeachを使わない.
            for (var i = 0; i < soundElements.Count; ++i)
            {
                soundElements[i].Update();
            }
        }

        private void ReleaseSoundSheet()
        {
            for (var i = 0; i < soundElements.Count; ++i)
            {
                if (!soundElements[i].FinishTime.HasValue)
                {
                    continue;
                }

                // 終了確認した時間から一定時間経過していたら解放.
                if (soundElements[i].FinishTime.Value + ReleaseTime < Time.realtimeSinceStartup)
                {
                    soundElements.RemoveAt(i);
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

        private void SoundEventCallback(string eventParam)
        {
            // イベント には ID やその他の情報が含まれる.
            if (onSoundEvent != null)
            {
                SoundEventParam param = null;

                try
                {
                    var strParams = eventParam.Split(SoundEventSeparator);

                    param = new SoundEventParam()
                    {
                        position = int.Parse(strParams[0]),
                        eventId = int.Parse(strParams[1]),
                        tag = strParams[4],
                    };
                }
                catch (Exception)
                {
                    Debug.LogErrorFormat("Invalid sound event.\nparam : {0}", eventParam);
                    throw;
                }

                onSoundEvent.OnNext(param);
            }
        }

        /// <summary> 再生通知 </summary>
        public IObservable<SoundElement> OnPlayAsObservable()
        {
            return onPlay ?? (onPlay = new Subject<SoundElement>());
        }

        /// <summary> 停止通知 </summary>
        public IObservable<SoundElement> OnStopAsObservable()
        {
            return onStop ?? (onStop = new Subject<SoundElement>());
        }

        /// <summary> 中断通知 </summary>
        public IObservable<SoundElement> OnPauseAsObservable()
        {
            return onPause ?? (onPause = new Subject<SoundElement>());
        }

        /// <summary> 復帰通知 </summary>
        public IObservable<SoundElement> OnResumeAsObservable()
        {
            return onResume ?? (onResume = new Subject<SoundElement>());
        }

        /// <summary> サウンドに埋め込まれたイベント通知 </summary>
        public IObservable<SoundEventParam> OnSoundEventAsObservable()
        {
            return onSoundEvent ?? (onSoundEvent = new Subject<SoundEventParam>());
        }
    }
}

#endif
