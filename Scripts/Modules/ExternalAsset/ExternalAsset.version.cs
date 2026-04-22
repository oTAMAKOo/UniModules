
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        //----- field -----

        // ファイル一覧の一時キャッシュ.
        private HashSet<string> filePathTemporaryCache = null;

        //----- property -----

        //----- method -----

        /// <summary> 更新が必要なアセット情報を取得. </summary>
        public async UniTask<AssetInfo[]> GetRequireUpdateAssetInfos(string groupName = null)
        {
            if (SimulateMode) { return new AssetInfo[0]; }

            if (assetInfoManifest == null) { return new AssetInfo[0]; }

            AssetInfo[] result = null;

            await UniTask.RunOnThreadPool(async () =>
            {
                var assetInfos = assetInfoManifest.GetAssetInfos(groupName)
                    .DistinctBy(x => x.FileName)
                    .ToArray();

                // ローカルファイル一覧とマニフェスト期待ハッシュの差分で更新要否を判定.

                var filePaths = await GetInstallDirectoryFilePaths(false);

                filePathTemporaryCache = new HashSet<string>();

                foreach (var file in filePaths)
                {
                    filePathTemporaryCache.Add(file);
                }

                var tasks = new List<UniTask<AssetInfo[]>>();

                var chunck = assetInfos.Chunk(500);

                foreach (var items in chunck)
                {
                    var task = UniTask.RunOnThreadPool(() => items.Where(x => IsRequireUpdate(x)).ToArray(), false);

                    tasks.Add(task);
                }

                var filterResults = await UniTask.WhenAll(tasks);

                result = filterResults.SelectMany(x => x).ToArray();

                filePathTemporaryCache = null;
            });

            return result;
        }

        /// <summary>
        /// 更新が必要か.
        /// (ファイル名にハッシュを埋め込む方式のためファイル存在確認で完結する)
        /// </summary>
        public bool IsRequireUpdate(AssetInfo assetInfo)
        {
            if (SimulateMode){ return false; }

            if (assetInfo == null){ return true; }

            var filePath = GetFilePath(InstallDirectory, assetInfo);

            // ファイルパスが取得できないアセットは更新要扱い.
            if (string.IsNullOrEmpty(filePath)){ return true; }

            #if UNITY_ANDROID

            if (LocalMode && filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
            {
                filePath = AndroidUtility.ConvertStreamingAssetsLoadPath(filePath);
            }

            #endif

            // ファイルの存在確認.
            if (filePathTemporaryCache != null)
            {
                return !filePathTemporaryCache.Contains(filePath);
            }

            return !File.Exists(filePath);
        }
    }
}
