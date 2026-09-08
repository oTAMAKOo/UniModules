
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_UTAGE

using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Utage;
using Modules.ExternalAssets;
using Modules.Sound;

namespace Modules.UtageExtension
{
    public sealed class ExternalAssetSoundAssetFile : AssetFileBase
    {
        //----- params -----

        //----- field -----

        private string resourcesPath = null;
        private string soundName = null;

        //----- property -----
        
        public CueInfo CueInfo { get; private set; }

        //----- method -----

        public ExternalAssetSoundAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData) : base(mangager, fileInfo, settingData)
        {
            var setting = settingData as IAssetFileSoundSettingData;

            if (setting != null)
            {
                resourcesPath = settingData.RowData.ParseCellOptional<string>("FileName", null);

                if (setting is AdvVoiceSetting)
                {                    
                    soundName = settingData.RowData.ParseCellOptional<string>("Voice", null);
                }
                else
                {
                    soundName = settingData.RowData.ParseCellOptional<string>("SoundName", null);
                }
            }
        }

        public override bool CheckCacheOrLocal()
        {
            return true;
        }

        public override IEnumerator LoadAsync(Action onComplete, Action onFailed)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                onFailed();
                yield break;
            }

            var updateCompleted = false;
            var updateHasError = false;

            ExternalAsset.UpdateAsset(resourcesPath)
                .ContinueWith(() => { updateCompleted = true; })
                .Forget(ex => { updateHasError = true; updateCompleted = true; });

            while (!updateCompleted) { yield return null; }

            if (updateHasError)
            {
                onFailed();
                yield break;
            }

            if (Priority != AssetFileLoadPriority.DownloadOnly)
            {
                CueInfo cueResult = null;
                var cueCompleted = false;
                var cueHasError = false;

                ExternalAsset.GetCueInfo(resourcesPath, soundName)
                    .ContinueWith(x => { cueResult = x; cueCompleted = true; })
                    .Forget(ex => { cueHasError = true; cueCompleted = true; });

                while (!cueCompleted)
                {
                    yield return null;
                }

                CueInfo = cueResult;

                if (CueInfo == null)
                {
                    IsLoadError = true;
                    onFailed();

                    yield break;
                }
            }

            IsLoadEnd = true;
            onComplete();
        }

        public override void Unload()
        {
            IsLoadEnd = false;
        }
    }
}

#endif
