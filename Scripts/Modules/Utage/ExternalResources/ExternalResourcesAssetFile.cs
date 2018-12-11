
#if ENABLE_UTAGE

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Utage;
using Extensions;
using Modules.ExternalResource;

namespace Modules.UtageExtension
{
    public abstract class ExternalResourcesAssetFile<T> : AssetFileBase where T : UnityEngine.Object
    {
        //----- params -----

        //----- field -----

        private string resourcesPath = null;

        //----- property -----

        public T Asset { get; protected set; }

        //----- method -----

        public ExternalResourcesAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData) : base(mangager, fileInfo, settingData)
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

            var updateYield = ExternalResources.UpdateAsset(resourcesPath).ToYieldInstruction(false);

            yield return updateYield;

            if (updateYield.HasError)
            {
                onFailed();
                yield break;
            }
            
            if (Priority != AssetFileLoadPriority.DownloadOnly)
            {
                var loadYield = ExternalResources.LoadAsset<T>(resourcesPath, false).ToYieldInstruction(false);

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
            ExternalResources.UnloadAssetBundle(resourcesPath);
        }

        protected virtual void OnLoadComplete(T asset){}
    }
}

#endif
