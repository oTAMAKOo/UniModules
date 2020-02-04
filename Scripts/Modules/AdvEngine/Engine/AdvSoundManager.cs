
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.ExternalResource;
using Modules.SoundManagement;

namespace Modules.AdvKit
{
    public class SoundInfo
    {
        public SoundType SoundType { get; private set; }
        public string ResourcePath { get; private set; }
        public string AcbName { get; private set; }
        public string CueName { get; private set; }

        public SoundInfo(SoundType soundType, string resourcePath, string acbName, string cueName)
        {
            SoundType = soundType;
            ResourcePath = resourcePath;
            AcbName = acbName;
            CueName = cueName;
        }
    }

    public abstract class AdvSoundManager : LifetimeDisposable
    {
        //----- params -----

        //----- field -----

        private SoundElement bgm = null;

        private Dictionary<string, SoundElement> se = null;
        private Dictionary<string, SoundElement> voice = null;
        private Dictionary<string, SoundElement> ambience = null;

        private Dictionary<string, SoundInfo> soundInfos = null;

        //----- property -----

        public bool IsEnable { get; set; }

        //----- method -----

        public AdvSoundManager()
        {
            soundInfos = new Dictionary<string, SoundInfo>();

            se = new Dictionary<string, SoundElement>();
            voice = new Dictionary<string, SoundElement>();
            ambience = new Dictionary<string, SoundElement>();

            IsEnable = true;
        }

        public SoundInfo Register(string soundIdentifier, SoundType soundType, string acbName, string cueName)
        {
            var resourcePath = string.Empty;

            if (string.IsNullOrEmpty(acbName))
            {
                acbName = cueName;
            }

            switch (soundType)
            {
                case SoundType.Bgm:
                    resourcePath = GetBgmResourcePath(acbName);
                    break;
                case SoundType.Se:
                    resourcePath = GetSeResourcePath(acbName);
                    break;
                case SoundType.Voice:
                    resourcePath = GetVoiceResourcePath(acbName);
                    break;
                case SoundType.Ambience:
                    resourcePath = GetAmbienceResourcePath(acbName);
                    break;
            }

            var soundInfo = new SoundInfo(soundType, resourcePath, acbName, cueName);

            soundInfos[soundIdentifier] = soundInfo;

            return soundInfo;
        }

        public SoundInfo GetSoundInfo(string identifier)
        {
            return soundInfos.GetValueOrDefault(identifier);
        }

        #region Bgm

        public void PlayBgm(string soundIdentifier)
        {
            if (!IsEnable) { return; }

            var soundInfo = soundInfos.FirstOrDefault(x => x.Key == soundIdentifier && x.Value.SoundType == SoundType.Bgm).Value;

            if (soundInfo == null) { return; }

            ExternalResources.GetCueInfo(soundInfo.ResourcePath, soundInfo.CueName)
                .Subscribe(x => bgm = SoundManagement.SoundManagement.Play(SoundType.Bgm, x))
                .AddTo(Disposable);
        }

        public void StopBgm()
        {
            if (bgm == null) { return; }

            SoundManagement.SoundManagement.Stop(bgm);
        }

        #endregion

        #region Se

        public void PlaySe(string identifier, string soundIdentifier)
        {
            PlaySound(identifier, soundIdentifier, SoundType.Se, se);
        }

        public void StopSe(string identifier)
        {
            StopSound(identifier, se);
        }

        #endregion

        #region Voice

        public void PlayVoice(string identifier, string soundIdentifier)
        {
            PlaySound(identifier, soundIdentifier, SoundType.Voice, voice);
        }

        public void StopVoice(string identifier)
        {
            StopSound(identifier, voice);
        }

        #endregion

        #region Ambience

        public void PlayAmbience(string identifier, string soundIdentifier)
        {
            PlaySound(identifier, soundIdentifier, SoundType.Ambience, ambience);
        }

        public void StopAmbience(string identifier)
        {
            StopSound(identifier, ambience);
        }

        #endregion

        private void PlaySound(string identifier, string soundIdentifier, SoundType soundType, Dictionary<string, SoundElement> soundElements)
        {
            if (!IsEnable) { return; }

            var soundInfo = soundInfos.FirstOrDefault(x => x.Key == soundIdentifier && x.Value.SoundType == soundType).Value;

            if (soundInfo == null) { return; }

            Action<SoundElement> onFinishSound = soundElement =>
            {
                soundElements.Remove(identifier);
            };

            Action<CueInfo> onCueReady = cue =>
            {
                var soundElement = SoundManagement.SoundManagement.Play(soundType, cue);

                soundElements.Add(identifier, soundElement);

                soundElement.OnFinishAsObservable()
                    .Subscribe(_ => onFinishSound(soundElement))
                    .AddTo(Disposable);
            };

            ExternalResources.GetCueInfo(soundInfo.ResourcePath, soundInfo.CueName)
                .Subscribe(x => onCueReady(x))
                .AddTo(Disposable);
        }

        private void StopSound(string identifier, Dictionary<string, SoundElement> soundElements)
        {
            if (!string.IsNullOrEmpty(identifier))
            {
                var soundElement = soundElements.GetValueOrDefault(identifier);

                SoundManagement.SoundManagement.Stop(soundElement);

                soundElements.Remove(identifier);
            }
        }

        public void StopAllSound()
        {
            StopBgm();

            se.ForEach(x => SoundManagement.SoundManagement.Stop(x.Value));
            se.Clear();

            voice.ForEach(x => SoundManagement.SoundManagement.Stop(x.Value));
            voice.Clear();

            ambience.ForEach(x => SoundManagement.SoundManagement.Stop(x.Value));
            ambience.Clear();
        }

        protected abstract string GetBgmResourcePath(string fileName);
        protected abstract string GetSeResourcePath(string fileName);
        protected abstract string GetVoiceResourcePath(string fileName);
        protected abstract string GetAmbienceResourcePath(string fileName);
    }
}

#endif
