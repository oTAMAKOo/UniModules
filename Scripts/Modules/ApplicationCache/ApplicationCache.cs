
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UniRx;
using Extensions;
using Modules.ExternalResource;

namespace Modules.ApplicationCache
{
    public static class ApplicationCache
    {
        //----- params -----

        private static readonly string[] CachePaths = new string[]
        {
                Application.persistentDataPath + "/",
                Application.temporaryCachePath + "/",
        };

        //----- field -----

        //----- property -----

        //----- method -----

        public static IObservable<Unit> Clean()
        {
            return Observable.FromCoroutine(() => CleanCore());
        }

        private static IEnumerator CleanCore()
        {
            var builder = new StringBuilder();

            #if ENABLE_CRIWARE_ADX

            // 未使用の音ファイルを解放.
            if (SoundManagement.SoundManagement.Exists)
            {
                SoundManagement.SoundManagement.Instance.ReleaseAll();
            }

            #endif

            #if ENABLE_CRIWARE_SOFDEC

            if (MovieManagement.MovieManagement.Exists)
            {
                MovieManagement.MovieManagement.ReleaseAll();
            }

            #endif

            yield return null;

            // キャッシュクリア.
            ExternalResources.Instance.CleanCache();

            yield return null;

            // その他のキャッシュ.
            foreach (var cachePath in CachePaths)
            {
                if (!Directory.Exists(cachePath)) { continue; }

                var fs = Directory.GetFiles(cachePath);

                foreach (var item in fs)
                {
                    builder.AppendLine(item);

                    try
                    {
                        var cFileInfo = new FileInfo(item);

                        // 読み取り専用属性がある場合は、読み取り専用属性を解除.
                        if ((cFileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            cFileInfo.Attributes = FileAttributes.Normal;
                        }

                        File.Delete(item);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                DirectoryUtility.DeleteEmpty(cachePath);

                yield return null;
            }

            if (!string.IsNullOrEmpty(builder.ToString()))
            {
                builder.Insert(0, "CleanCache :");

                Debug.Log(builder.ToString());
            }
        }
	}
}
