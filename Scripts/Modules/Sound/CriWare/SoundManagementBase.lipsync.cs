
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_CRIWARE_LIPSYNC

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using CriWare;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Sound
{
    public partial class SoundManagementBase<TInstance, TSound>
    {
        //----- params -----

        //----- field -----

        private Dictionary<SoundElement, CriAtomExPlayer> lipSyncPlayers = null;

        //----- property -----

        //----- method -----

        private void InitializeLipSync()
        {
            lipSyncPlayers = new Dictionary<SoundElement, CriAtomExPlayer>();

            OnReleaseAsObservable()
                .Subscribe(x => ReleaseLipSyncElement(x))
                .AddTo(Disposable);

            OnPauseAllAsObservable()
                .Subscribe(_ => PauseAllLipSyncPlayer())
                .AddTo(Disposable);

            OnResumeAllAsObservable()
                .Subscribe(x => ResumeAllLipSyncPlayer(x))
                .AddTo(Disposable);
        }

        /// <summary> 内蔵アセットサウンドを再生. </summary>
        public SoundElement LipSyncPlay(SoundType type, TSound cue, float? volume = null)
        {
            var info = GetCueInfo(cue);

            return PlayLipSyncSoundCore(type, info, volume);
        }

        /// <summary> 外部アセットのサウンドを再生. </summary>
        public SoundElement LipSyncPlay(SoundType type, CueInfo info, float? volume = null)
        {
            return PlayLipSyncSoundCore(type, info, volume);
        }

        /// <summary> リップシンクのサウンドを再生. </summary>
        private SoundElement PlayLipSyncSoundCore(SoundType type, CueInfo info, float? volume)
        {
            if (info == null){ return null; }

            var lipSyncPlayer = new CriAtomExPlayer();

            var element = PlaySoundCore(lipSyncPlayer, type, info, volume);

            if (element != null)
            {
                var player = element.GetPlayer();

                if (player == lipSyncPlayer)
                {
                    lipSyncPlayers.Add(element, lipSyncPlayer);
                }
                else
                {
                    lipSyncPlayer.Dispose(); 
                }

                element.OnFinishAsObservable()
                    .Subscribe(_ => ReleaseLipSyncElement(element))
                    .AddTo(Disposable);
            }
            else
            {
                lipSyncPlayer.Dispose(); 
            }

            return element;
        }

        private void ReleaseLipSyncElement(SoundElement element)
        {
            if (element == null){ return; }

            if (!lipSyncPlayers.ContainsKey(element)){ return; }

            var player = element.GetPlayer();

            player.Dispose();

            lipSyncPlayers.Remove(element);
        }

        private void PauseAllLipSyncPlayer()
        {
            lipSyncPlayers.Values.ForEach(x => x.Pause());
        }

        private void ResumeAllLipSyncPlayer(CriAtomEx.ResumeMode resumeMode)
        {
            lipSyncPlayers.Values.ForEach(x => x.Resume(resumeMode));
        }
    }
}

#endif