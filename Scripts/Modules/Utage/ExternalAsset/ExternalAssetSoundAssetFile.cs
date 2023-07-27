
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_UTAGE

using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UniRx;
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

            var updateYield = ExternalAsset.UpdateAsset(resourcesPath)
				.ToObservable()
				.ToYieldInstruction();

            yield return updateYield;

            if (updateYield.HasError)
            {
                onFailed();
                yield break;
            }

            if (Priority != AssetFileLoadPriority.DownloadOnly)
            {
                var cueYield = ExternalAsset.GetCueInfo(resourcesPath, soundName)
					.ToObservable()
					.ToYieldInstruction();

                while (!cueYield.IsDone)
                {
                    yield return null;
                }

                CueInfo = cueYield.Result;

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
