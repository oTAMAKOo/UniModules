
using UnityEngine;
using System;
using System.IO;
using Extensions;

namespace Modules.AssetBundles
{
    public sealed partial class AssetBundleManager
    {
        // シーク読み込みアセットバンドル.
        private  class SeekableAssetBundle : IDisposable
        {
            public AssetBundle assetBundle = null;

            public FileStream fileStream = null;

            public SeekableCryptoStream cryptoStream = null;

            public SeekableAssetBundle(AssetBundle assetBundle, FileStream fileStream, SeekableCryptoStream cryptoStream)
            {
                this.assetBundle = assetBundle;
                this.fileStream = fileStream;
                this.cryptoStream = cryptoStream;
            }

            public SeekableAssetBundle(SeekableAssetBundle seekableAssetBundle)
            {
                this.assetBundle = seekableAssetBundle.assetBundle;
                this.fileStream = seekableAssetBundle.fileStream;
                this.cryptoStream = seekableAssetBundle.cryptoStream;
            }
            
            ~SeekableAssetBundle()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (assetBundle != null)
                {
                    assetBundle.Unload(false);
                    assetBundle = null;
                }

                if (fileStream != null)
                {
                    fileStream.Dispose();
                    fileStream = null;
                }

                if (cryptoStream != null)
                {
                    cryptoStream.Dispose();
                    cryptoStream = null;
                }

                GC.SuppressFinalize(this);
            }
        }

        // 読み込み済みアセットバンドル.
        private sealed class LoadedAssetBundle : SeekableAssetBundle
        {
            public int referencedCount = 0;

            public LoadedAssetBundle(SeekableAssetBundle seekableAssetBundle) : base(seekableAssetBundle)
            {
                this.referencedCount = 1;
            }
        }
    }
}