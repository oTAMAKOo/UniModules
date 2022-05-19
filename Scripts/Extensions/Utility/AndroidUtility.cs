
//#if UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Modules.Net.WebRequest;

namespace Extensions
{
    public static class AndroidUtility
    {
        //----- params -----

        //----- field -----

		//----- property -----

        //----- method -----

		// ※ AndroidではstreamingAssetsPathがWebRequestからしかアクセスできないのでtemporaryCachePathにファイルを複製する.
		public static async UniTask<bool> CopyStreamingToTemporary(string filePath)
		{
			filePath = PathUtility.ConvertPathSeparator(filePath);

			if (!filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
			{
				Debug.LogErrorFormat("Not streamingAssetsPath file.\n{0}", filePath);

				return false;
			}

			using (var webRequest = UnityWebRequest.Get(filePath))
			{
				var operation = webRequest.SendWebRequest();

				while (!operation.isDone)
				{
					await UniTask.NextFrame();
				}

				if (webRequest.HasError())
				{
					Debug.LogError("File load error : " + webRequest.error);
				}
				else
				{
					var path = ConvertStreamingAssetsLoadPath(filePath);

					var directory = Directory.GetParent(path);

					if (!directory.Exists)
					{
						directory.Create();
					}

					using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
					{
						using (var sw = new BinaryWriter(fs))
						{
							sw.Write(webRequest.downloadHandler.data);
						}
					}
				}
			}

			return true;
		}

		public static string ConvertStreamingAssetsLoadPath(string filePath)
		{
			filePath = PathUtility.ConvertPathSeparator(filePath);

			if (filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
			{
				var paths = new string[] { UnityPathUtility.TemporaryCachePath, "Embedded", filePath.Replace(UnityPathUtility.StreamingAssetsPath, string.Empty) };

				filePath = PathUtility.Combine(paths);
			}

			return filePath;
		}
	}
}

//#endif