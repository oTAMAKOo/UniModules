#if UNITY_ANDROID

using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.CriWare;

namespace Modules.Movie
{
	public sealed partial class MovieManagement
	{
		//----- params -----

		//----- field -----

		//----- property -----

		//----- method -----

		/// <summary>
		/// StreamingAssetsにある内蔵ファイルをTemporaryCachePathへ複製.
		/// 引数のversionが同じ間は複製を実行しない.
		/// </summary>
		private async UniTask<ManaInfo> PrepareInternalFile(ManaInfo movieInfo)
		{
			var appVersion = Application.version;

			var temporaryVersionKey = GetType().FullName + $"-temporaryVersion-{movieInfo.UsmPath}";

			var requireUpdate = SecurePrefs.GetString(temporaryVersionKey) != appVersion;
			
			var embeddedFilePath = Path.ChangeExtension(movieInfo.UsmPath, CriAssetDefinition.UsmExtension);

			var temporaryFilePath = AndroidUtility.ConvertStreamingAssetsLoadPath(embeddedFilePath);
			
			// バージョンが変わったか or ファイルが存在しない場合は更新.
			if (requireUpdate || !File.Exists(temporaryFilePath))
			{
				var result = await AndroidUtility.CopyStreamingToTemporary(embeddedFilePath);

				if (result)
				{
					SecurePrefs.SetString(temporaryVersionKey, appVersion);
				}
			}

			return new ManaInfo(temporaryFilePath);
		}
	}
}

#endif
