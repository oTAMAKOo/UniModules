
using UnityEditor;
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UniRx;

namespace Modules.Devkit.Build
{
    public static class BuiltInAssets
    {
        //----- params -----

        private const int FrameReadLine = 250;

        public class BuiltInAssetInfo
        {
            public string assetPath { get; private set; }
            public float size { get; private set; }
            public float ratio { get; private set; }

            public BuiltInAssetInfo(string line)
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

        public static IObservable<BuiltInAssetInfo[]> CollectBuiltInAssets(string logFilePath)
        {
            var progress = new ScheduledNotifier<float>();
            progress.Subscribe(prog => EditorUtility.DisplayProgressBar("progress", "Collect built-in assets from logfile.", prog));

            return Observable.FromCoroutine<BuiltInAssetInfo[]>(observer => CollectBuiltInAssetsInternal(observer, logFilePath, progress))
                .Do(x => EditorUtility.ClearProgressBar());
        }

        private static IEnumerator CollectBuiltInAssetsInternal(IObserver<BuiltInAssetInfo[]> observer, string logFilePath, IProgress<float> progress)
        {
            var builtInAssets = new List<BuiltInAssetInfo>();

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

                    builtInAssets.Clear();

                    while (!sr.EndOfStream && (line = sr.ReadLine()).Contains("%"))
                    {
                        // プロジェクト内のAssetはAssets/から始まる.
                        if (!line.Contains("Assets/")) { continue; }

                        builtInAssets.Add(new BuiltInAssetInfo(line));
                    }
                }

                progress.Report(1f);
            }

            observer.OnNext(builtInAssets.ToArray());
            observer.OnCompleted();
        }
    }
}
