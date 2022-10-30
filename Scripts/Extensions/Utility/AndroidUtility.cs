
#if UNITY_ANDROID

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Modules.Net.WebDownload;
using Modules.Net.WebRequest;

namespace Extensions
{
    public static class AndroidUtility
    {
        //----- params -----

		private const int MaxCopyCount = 5;

        private sealed class CopyBuffer
        {
            public bool use = false;
            public byte[] buffer = new byte[256 * 1024];
        }

        //----- field -----

		private static List<string> copyQueueing = null;
        
        private static List<CopyBuffer> copyBuffers = null;

		//----- property -----

        //----- method -----

        static AndroidUtility()
        {
            copyQueueing = new List<string>();
            copyBuffers = new List<CopyBuffer>();
        }

		// ※ AndroidではstreamingAssetsPathがWebRequestからしかアクセスできないのでtemporaryCachePathにファイルを複製する.
		public static async UniTask<bool> CopyStreamingToTemporary(string filePath)
		{
			filePath = PathUtility.ConvertPathSeparator(filePath);

			if (!filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
			{
				Debug.LogErrorFormat("Not streamingAssetsPath file.\n{0}", filePath);

				return false;
			}

			if (copyQueueing.Contains(filePath)){ return true; }

            copyQueueing.Add(filePath);

            CopyBuffer copyBuffer = null;

            while (true)
            {
                UpdateCopyBuffer();

                copyBuffer = copyBuffers.FirstOrDefault(x => !x.use);

                if (copyBuffer != null){ break; }

                await UniTask.NextFrame();
            }

            var copyPath = ConvertStreamingAssetsLoadPath(filePath);

            var directory = Directory.GetParent(copyPath);

            if (!directory.Exists)
            {
                directory.Create();
            }

            using (var webRequest = UnityWebRequest.Get(filePath))
			{
				webRequest.downloadHandler = new FileDownloadHandler(copyPath, copyBuffer.buffer);

				var operation = webRequest.SendWebRequest();

				while (!operation.isDone)
				{
					await UniTask.NextFrame();
				}

				if (webRequest.HasError())
				{
					Debug.LogError($"File copy error : \nfrom :{filePath}\nto : {copyPath}\n\n{webRequest.error}");
				}
			}

            Debug.Log($"from : {filePath}\nto : {copyPath}");

            copyBuffer.use = false;

            copyQueueing.Remove(filePath);

            UpdateCopyBuffer();

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

        private static void UpdateCopyBuffer()
        {
            var requireCount = Math.Min(MaxCopyCount, copyQueueing.Count);

            // 余っているバッファ削除.
            if (requireCount < copyBuffers.Count)
            {
                var deleteCount = copyBuffers.Count - requireCount;

                var unuseBuffers = copyBuffers.Where(x => !x.use).ToArray();

                for (var i = 0; i < unuseBuffers.Length; i++)
                {
                    if (deleteCount <= i){ break; }

                    copyBuffers.Remove(unuseBuffers[i]);
                }
            }
            // 足りないバッファ追加.
            else if(copyBuffers.Count < requireCount)
            {
                var addCount = requireCount - copyBuffers.Count;

                for (var i = 0; i < addCount; i++)
                {
                    copyBuffers.Add(new CopyBuffer());
                }
            }
        }
	}
}

#endif