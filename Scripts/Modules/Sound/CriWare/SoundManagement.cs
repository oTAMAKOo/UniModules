
#if ENABLE_CRIWARE_ADX

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using CriWare;
using Cysharp.Threading.Tasks;
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

    public sealed partial class SoundManagement : SoundManagementBase<SoundManagement, SoundParam, SoundElement>
    {
        //----- params -----

        private const float DefaultReleaseTime = 30f;

		//----- field -----

        private CriAtomExPlayer player = null;

        private Dictionary<string, SoundSheet> managedSoundSheets = null;
		
        // サウンドイベント.
        private Subject<CriAtomExSequencer.CriAtomExSequenceEventInfo> onSoundEvent = null;

		private bool initialized = false;

        //----- property -----

        public CriAtomExPlayer Player { get { return player; } }

        public float ReleaseTime { get; set; }

        //----- method -----

        private SoundManagement()
        {
            ReleaseTime = DefaultReleaseTime;
        }

        public void Initialize(SoundParam defaultSoundParam)
        {
            if (initialized) { return; }

			player = new CriAtomExPlayer();

            OnInitialize(defaultSoundParam);

            managedSoundSheets = new Dictionary<string, SoundSheet>();

            // デフォルトのサウンド設定を適用.
            ApplySoundParam();

            // サウンドイベントを受信.

            CriAtomExSequencer.EventCallback soundEventCallback = (ref CriAtomExSequencer.CriAtomExSequenceEventInfo info) =>
            {
                if (onSoundEvent != null)
                {
                    onSoundEvent.OnNext(info);
                }
            };

            CriAtomExSequencer.OnCallback += soundEventCallback;

			// 一定周期で未使用状態になったAcbの解放を行う.
            Observable.Interval(TimeSpan.FromSeconds(5f))
                .Subscribe(_ => ReleaseSoundSheet())
                .AddTo(Disposable);

			// パラメータ更新通知.
			OnUpdateParamAsObservable()
				.Subscribe(x => ApplySoundParam(x))
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
        /// InternalAsset内のサウンドを再生.
        /// </summary>
        public SoundElement Play(SoundType type, Sounds.Cue cue)
        {
            var soundParam = GetSoundParam(type);
            var info = Sounds.GetCueInfo(cue);

			#if !UNITY_EDITOR && UNITY_ANDROID

            var assetPath = AndroidUtility.ConvertStreamingAssetsLoadPath(info.FilePath);

            info = new CueInfo(assetPath, info.CueSheet, info.Cue, info.HasAwb, info.Summary);

			#endif

            if (soundParam != null && soundParam.cancelIfPlaying)
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
        ///  ExternalAsset内のサウンドを再生.
        /// </summary>
        public SoundElement Play(SoundType type, CueInfo info)
        {
            if (info == null) { return null; }

            var soundParam = GetSoundParam(type);

            SoundElement element = null;

            if (soundParam != null && soundParam.cancelIfPlaying)
            {
                element = FindPlayingElement(type, info);

                if (element != null)
                {
                    return element;
                }
            }

            element = GetSoundElement(info, type);

            if (element == null) { return null; }

            // 管理対象に追加.
            soundElements.Add(element);

            // 音量設定.
            SetVolume(element, soundParam.volume);

			PlaySoundElement(element).Forget();

            if (onPlay != null)
            {
                onPlay.OnNext(element);
            }

			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Play : {element.CueInfo.Cue} ({element.CueInfo.CueSheet})");

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

			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Pause : {element.CueInfo.Cue} ({element.CueInfo.CueSheet})");
        }

        /// <summary> 全サウンド中断 </summary>
        public void PauseAll()
        {
            if (soundElements.IsEmpty()) { return; }

            player.Pause();

            foreach (var element in soundElements)
            {
                element.Update();

                if (onPause != null)
                {
                    onPause.OnNext(element);
                }
            }

			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "All sound pause.");
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

			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Resume : {element.CueInfo.Cue} ({element.CueInfo.CueSheet})");
        }

        /// <summary> 全サウンド復帰 </summary>
        public void ResumeAll(CriAtomEx.ResumeMode resumeMode = CriAtomEx.ResumeMode.AllPlayback)
        {
            if (soundElements.IsEmpty()) { return; }

            player.Resume(resumeMode);

            foreach (var element in soundElements)
            {
                element.Update();

                if (onResume != null)
                {
                    onResume.OnNext(element);
                }
            }

			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "All sound resume.");
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

			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Stop : {element.CueInfo.Cue} ({element.CueInfo.CueSheet})");
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

			UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "All sound stopped.");
        }

        /// <summary> 個別に音量変更. </summary>
        public void SetVolume(SoundElement element, float value)
        {
            player.SetVolume(value);
            player.Update(element.GetPlayback());

            // デフォルトに戻す.
            ApplySoundParam();
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
            ApplySoundParam(element.Type);

            player.Update(element.GetPlayback());

            // デフォルトに戻す.
            ApplySoundParam();
        }

        /// <summary> CriAtomPlayerに再生設定を適用. </summary>
        private void ApplySoundParam(SoundType? type = null)
        {
            var param = type != null ? GetSoundParam(type.Value) : defaultSoundParam;

			if (param != null)
			{
	            player.SetVolume(param.volume);
			}
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


                if (LogEnable && UnityConsole.Enable)
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
            ApplySoundParam(type);

            // セットされた音声データの再生準備を開始.
            var playback = player.Prepare();

            // 再生失敗ならRemovedが返る.
            if (playback.GetStatus() == CriAtomExPlayback.Status.Removed) { return null; }

            // デフォルトに戻す.
            ApplySoundParam();

            var element = new SoundElement(type, soundSheet, info, playback);

            return element;
        }

        private async UniTask PlaySoundElement(SoundElement element)
        {
            var playback = element.GetPlayback();

            // 再生準備完了待ち.
            while (playback.GetStatus() != CriAtomExPlayback.Status.Playing)
            {
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

		/// <summary> サウンドに埋め込まれたイベント通知 </summary>
        public IObservable<CriAtomExSequencer.CriAtomExSequenceEventInfo> OnSoundEventAsObservable()
        {
            return onSoundEvent ?? (onSoundEvent = new Subject<CriAtomExSequencer.CriAtomExSequenceEventInfo>());
        }
    }
}

#endif
