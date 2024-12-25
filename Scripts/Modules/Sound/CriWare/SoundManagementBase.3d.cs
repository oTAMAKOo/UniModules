
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_CRIWARE_POS3D

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

        private Dictionary<SoundElement, CriAtomExPlayer> pos3dPlayers = null;

        //----- property -----

        //----- method -----

        private void Initialize3D()
        {
            pos3dPlayers = new Dictionary<SoundElement, CriAtomExPlayer>();

            OnReleaseAsObservable()
                .Subscribe(x => Release3DElement(x))
                .AddTo(Disposable);

            OnPauseAllAsObservable()
                .Subscribe(_ => PauseAll3D())
                .AddTo(Disposable);

            OnResumeAllAsObservable()
                .Subscribe(x => ResumeAll3D(x))
                .AddTo(Disposable);
        }

        /// <summary> 内蔵アセットサウンドを再生. </summary>
        public SoundElement Play3D(SoundType type, TSound cue, Vector3 position , float? volume = null)
        {
            var info = GetCueInfo(cue);

            return Play3DSoundCore(type, info, position, volume);
        }

        /// <summary> 外部アセットのサウンドを再生. </summary>
        public SoundElement Play3D(SoundType type, CueInfo info, Vector3 position, float? volume = null)
        {
            return Play3DSoundCore(type, info, position, volume);
        }

        /// <summary> 3Dサウンドを再生. </summary>
        private SoundElement Play3DSoundCore(SoundType type, CueInfo info, Vector3 position, float? volume)
        {
            if (info == null){ return null; }

            var pos3dPlayer = new CriAtomExPlayer();

            pos3dPlayer.SetPanType(CriAtomEx.PanType.Pos3d);

            var element = PlaySoundCore(pos3dPlayer, type, info, volume);

            if (element != null)
            {
                var player = element.GetPlayer();

                if (player == pos3dPlayer)
                {
                    var source = new CriAtomEx3dSource();
                    
                    source.SetPosition(position.x, position.y, position.z);
                    source.Update();

                    element.Set3dSource(source);

                    player.UpdateAll();

                    pos3dPlayers.Add(element, pos3dPlayer);
                }
                else
                {
                    pos3dPlayer.Dispose();
                }

                element.OnFinishAsObservable()
                    .Subscribe(x => Release3DElement(element))
                    .AddTo(Disposable);
            }

            return element;
        }

        private void Release3DElement(SoundElement element)
        {
            if (element == null){ return; }

            if (!pos3dPlayers.ContainsKey(element)){ return; }
            
            var player = element.GetPlayer();

            player.Dispose();

            var pos3dSource = element.Get3dSource();

            pos3dSource.Dispose();

            pos3dPlayers.Remove(element);
        }

        public void PauseAll3D()
        {
            pos3dPlayers.Values.ForEach(x => x.Pause());
        }

        public void ResumeAll3D(CriAtomEx.ResumeMode resumeMode)
        {
            pos3dPlayers.Values.ForEach(x => x.Resume(resumeMode));
        }
    }
}

#endif