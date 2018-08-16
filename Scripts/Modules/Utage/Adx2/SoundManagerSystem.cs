
#if ENABLE_CRIWARE

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Utage;
using Extensions;
using Modules.SoundManagement;

using SoundType = Modules.SoundManagement.SoundType;

namespace Modules.UtageExtension
{
    public class SoundManagerSystem : SoundManagerSystemInterface
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public SoundManager SoundManager { get; private set; }

        public bool IsLoading
        {
            get { return false; }
        }

        //----- method -----

        public SoundManagerSystem(){}

        public void Init(SoundManager soundManager, List<string> saveStreamNameList)
        {
            SoundManager = soundManager;
        }

        public AudioSource GetAudioSource(string groupName, string label)
        {
            Debug.LogError("Not Support AudioSource");

            return null;
        }

        public float GetGroupVolume(string groupName)
        {
            return 1f;
        }

        public float GetMasterVolume(string groupName)
        {
            return 1f;
        }

        public float GetSamplesVolume(string groupName, string label)
        {
            return 1f;
        }

        public bool IsMultiPlay(string groupName)
        {
            return false;
        }

        public bool IsPlaying(string groupName, string label)
        {
            return false;
        }

        public void Play(string groupName, string label, SoundData soundData, float fadeInTime, float fadeOutTime)
        {
            var soundManagement = SoundManagement.SoundManagement.Instance;

            if(soundData.Clip != null) { return; }

            var soundAssetFile = soundData.File as ExternalResourcesSoundAssetFile;

            if(soundAssetFile == null) { return; }
            
            switch (groupName)
            {
                case SoundManager.IdBgm:
                    soundManagement.Play(SoundType.Bgm, soundAssetFile.CueInfo);
                    break;

                case SoundManager.IdAmbience:
                    soundManagement.Play(SoundType.Ambience, soundAssetFile.CueInfo);
                    break;

                case SoundManager.IdVoice:
                    soundManagement.Play(SoundType.Voice, soundAssetFile.CueInfo);
                    break;

                case SoundManager.IdSe:
                    soundManagement.Play(SoundType.Se, soundAssetFile.CueInfo);
                    break;
            }
        }

        public void SetGroupVolume(string groupName, float volume)
        {

        }

        public void SetMasterVolume(string groupName, float volume)
        {

        }

        public void SetMultiPlay(string groupName, bool multiPlay)
        {

        }

        public void Stop(string groupName, string label, float fadeTime)
        {

        }

        public void StopAll(float fadeTime)
        {

        }

        public void StopGroup(string groupName, float fadeTime)
        {

        }

        public void StopGroupIgnoreLoop(string groupName, float fadeTime)
        {

        }

        public void WriteSaveData(BinaryWriter writer)
        {
            
        }

        public void ReadSaveDataBuffer(BinaryReader reader)
        {
            
        }
    }
}

#endif
