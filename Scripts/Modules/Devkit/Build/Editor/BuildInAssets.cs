﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Extensions;
using UniRx;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Build
{
    public static class BuildInAssets
    {
        //----- params -----

        private const int FrameReadLine = 250;

        public class BuildInAssetInfo
        {
            public string assetPath { get; private set; }
            public float size { get; private set; }
            public float ratio { get; private set; }

            public BuildInAssetInfo(string line)
            {
                var delimiterChars = new char[] { ' ', '\t' };

                var split = line.Split(delimiterChars)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray();

                size = Convert.ToSingle(split[0]);

                switch (split[1])
                {
                    case "mb":
                        size *= 1024f;
                        break;
                    case "kb":
                        size *= 1f;
                        break;
                }

                ratio = Convert.ToSingle(split[2].Replace("%", string.Empty));
                assetPath = line.Substring(line.IndexOf("Assets/", StringComparison.Ordinal));
            }

            public string GetSizeText()
            {
                return 1024f <= size ? string.Format("{0:0.0}mb", size / 1024f) : string.Format("{0:0.0}kb", size);
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static IObservable<BuildInAssetInfo[]> CollectBuildInAssets(string logFilePath)
        {
            var progress = new ScheduledNotifier<float>();
            progress.Subscribe(prog => EditorUtility.DisplayProgressBar("progress", "Collect build in assets from logfile.", prog));

            return Observable.FromCoroutine<BuildInAssetInfo[]>(observer => CollectBuildInAssetsInternal(observer, logFilePath, progress))
                .Do(x => EditorUtility.ClearProgressBar());
        }

        private static IEnumerator CollectBuildInAssetsInternal(IObserver<BuildInAssetInfo[]> observer, string logFilePath, IProgress<float> progress)
        {
            var buildInAssets = new List<BuildInAssetInfo>();

            if (File.Exists(logFilePath))
            {
                var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var sr = new StreamReader(fs);

                var line = string.Empty;
                var count = 0;

                progress.Report(0f);

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();

                    if (FrameReadLine <= count++)
                    {
                        count = 0;

                        var val = (float)sr.BaseStream.Position / sr.BaseStream.Length;
                        progress.Report(val);
                        yield return null;
                    }

                    if (!line.StartsWith("Used Assets and files")) { continue; }

                    buildInAssets.Clear();

                    while (!sr.EndOfStream && (line = sr.ReadLine()).Contains("%"))
                    {
                        // プロジェクト内のAssetはAssets/から始まる.
                        if (!line.Contains("Assets/")) { continue; }

                        buildInAssets.Add(new BuildInAssetInfo(line));
                    }
                }

                progress.Report(1f);
            }

            observer.OnNext(buildInAssets.ToArray());
            observer.OnCompleted();
        }
    }
}
