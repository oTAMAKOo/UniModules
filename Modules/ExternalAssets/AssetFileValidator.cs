
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.ExternalAssets
{
    /// <summary> ファイルをアセット情報のサイズ・CRCと照合する. </summary>
    public static class AssetFileValidator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> サイズ・CRCが一致しない場合は InvalidDataException を送出する. アセット情報に値が無い項目は照合しない. </summary>
        public static async UniTask Validate(string filePath, AssetInfo assetInfo, CancellationToken cancelToken = default)
        {
            if (assetInfo == null){ return; }

            if (0 < assetInfo.Size)
            {
                var fileSize = new FileInfo(filePath).Length;

                if (fileSize != assetInfo.Size)
                {
                    throw new InvalidDataException($"File size mismatch.\nResourcePath : {assetInfo.ResourcePath}\nExpected : {assetInfo.Size}\nActual : {fileSize}");
                }
            }

            if (!string.IsNullOrEmpty(assetInfo.CRC))
            {
                // ファイル全体を読み込むためスレッドプールで計算する.
                var crc = await UniTask.RunOnThreadPool(() => FileUtility.GetCRC(filePath), cancellationToken: cancelToken);

                if (!string.Equals(crc, assetInfo.CRC, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"File CRC mismatch.\nResourcePath : {assetInfo.ResourcePath}\nExpected : {assetInfo.CRC}\nActual : {crc}");
                }
            }
        }
    }
}
