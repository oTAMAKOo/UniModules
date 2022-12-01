
#if ENABLE_UTAGE

using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UniRx;
using Utage;
using Extensions;
using Modules.ExternalAssets;

namespace Modules.UtageExtension
{
    public abstract class ExternalAssetAssetFile<T> : AssetFileBase where T : UnityEngine.Object
    {
        //----- params -----

        //----- field -----

        private string resourcesPath = null;

        //----- property -----

        public T Asset { get; protected set; }

        //----- method -----

        public ExternalAssetAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData) : base(mangager, fileInfo, settingData)
        {
            var setting = settingData as IAssetFileSettingData;

            if(setting != null)
            {
                resourcesPath = settingData.RowData.ParseCellOptional<string>("FileName", null);
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
				.ToYieldInstruction(false);

            yield return updateYield;

            if (updateYield.HasError)
            {
                onFailed();
                yield break;
            }
            
            if (Priority != AssetFileLoadPriority.DownloadOnly)
            {
                var loadYield = ExternalAsset.LoadAsset<T>(resourcesPath, false)
					.ToObservable()
					.ToYieldInstruction(false);

                yield return loadYield;

                if (loadYield.HasError)
                {
                    onFailed();
                    yield break;
                }

                Asset = loadYield.Result;
                OnLoadComplete(Asset);

                if (Asset == null)
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
            ExternalAsset.UnloadAssetBundle(resourcesPath);
        }

        protected virtual void OnLoadComplete(T asset){}
    }
}

#endif
