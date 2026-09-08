
#if ENABLE_UTAGE

using System;
using System.Collections;
using Cysharp.Threading.Tasks;
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
                T loadResult = null;
                var loadCompleted = false;
                var loadHasError = false;

                ExternalAsset.LoadAsset<T>(resourcesPath)
                    .ContinueWith(x => { loadResult = x; loadCompleted = true; })
                    .Forget(ex => { loadHasError = true; loadCompleted = true; });

                while (!loadCompleted) { yield return null; }

                if (loadHasError)
                {
                    onFailed();
                    yield break;
                }

                Asset = loadResult;
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
