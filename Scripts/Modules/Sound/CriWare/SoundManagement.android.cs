﻿
#if UNITY_ANDROID

using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.CriWare;

namespace Modules.Sound
{
    public sealed partial class SoundManagement
    {
        //----- params -----

		//----- field -----

        //----- property -----

        //----- method -----

		/// <summary>
		/// StreamingAssetsにある内蔵ファイルをTemporaryCachePathへ複製.
		/// 引数のversionが同じ間は複製を実行しない.
		/// </summary>
		public async UniTask PrepareInternalFiles(string version)
		{
			var temporaryVersionKey = GetType().FullName + "-temporaryVersion";

			var requireUpdate = SecurePrefs.GetString(temporaryVersionKey) != version;

			var internalFileInfos = Sounds.GetInternalFileInfo();
			
			var chunckedFileInfos = internalFileInfos.Chunk(10);

			void AddCopyTask(string embeddedFilePath, List<UniTask> tasks)
			{
				var temporaryFilePath = AndroidUtility.ConvertStreamingAssetsLoadPath(embeddedFilePath);

				// バージョンが変わったか or ファイルが存在しない場合は更新.
				if (requireUpdate || !File.Exists(temporaryFilePath))
				{
					var task = UniTask.Defer(() => AndroidUtility.CopyStreamingToTemporary(embeddedFilePath));

					tasks.Add(task);
				}
			};

			foreach (var infos in chunckedFileInfos)
			{
				var tasks = new List<UniTask>();

				foreach (var info in infos)
				{
					var filePath = PathUtility.Combine(UnityPathUtility.StreamingAssetsPath, info.Item1);

					// Acbファイル.
					AddCopyTask(filePath + CriAssetDefinition.AcbExtension, tasks);

					// Awbファイル.
					if (info.Item2)
					{
						AddCopyTask(filePath + CriAssetDefinition.AwbExtension, tasks);
					}
				}

				await UniTask.WhenAll(tasks);
			}

			SecurePrefs.SetString(temporaryVersionKey, version);
		}
    }
}

#endif