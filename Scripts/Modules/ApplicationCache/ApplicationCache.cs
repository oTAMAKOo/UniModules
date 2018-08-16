﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UniRx;
using Extensions;
using Modules.ExternalResource;

#if ENABLE_CRIWARE

using Modules.CriWare;

#endif

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

            builder.AppendLine("CleanCache :");

            #if ENABLE_CRIWARE

            // 未使用の音ファイルを解放.
            if (SoundManagement.SoundManagement.Exists)
            {
                SoundManagement.SoundManagement.Instance.ReleaseAll();
            }

            #endif

            yield return null;

            if (ExternalResources.Exists)
            {
                // ExternalResourcesのキャッシュクリア.
                ExternalResources.Instance.CleanCache();
            }
            else
            {
                // アセットバンドルキャッシュクリア.
                Caching.ClearCache();
            }

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

                yield return null;
            }

#if ENABLE_CRIWARE

            // Criキャッシュクリア.
            CriAssetManager.CleanCache();

            yield return null;

#endif

            Debug.Log(builder.ToString());
        }
	}
}
