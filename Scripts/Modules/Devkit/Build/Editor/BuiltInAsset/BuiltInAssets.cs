
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Extensions;

namespace Modules.Devkit.Build
{
    public static class BuiltInAssets
    {
        //----- params -----

        private const int FrameReadLine = 250;

        public sealed class BuiltInAssetInfo
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
				return 1024f <= size ? $"{size / 1024f:0.0}mb" : $"{size:0.0}kb";
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static BuiltInAssetInfo[] CollectBuiltInAssets(string logFilePath)
        {
            var builtInAssets = new List<BuiltInAssetInfo>();

            if (File.Exists(logFilePath))
            {
                var lines = File.ReadAllLines(logFilePath);

                // 最後尾から検索する為反転する.
                lines = lines.Reverse().ToArray();

                var count = 0;
                var collect = false;

                void DisplayProgress(int lineNumber)
                {
                    if (FrameReadLine <= count++)
                    {
                        count = 0;

                        var progress = (float)lineNumber / lines.Length;

                        EditorUtility.DisplayProgressBar("progress", "Collect built-in assets from logfile.", progress);
                    }
                }

                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    DisplayProgress(i);

                    if (collect)
                    {
                        // 開始位置まで読み込んだので終了.
                        if (line.StartsWith("Used Assets and files")) { break; }
                            
                        // プロジェクト内のAssetはAssets/から始まる.
                        if (!line.Contains("Assets/")) { continue; }

                        if (line.Contains("%"))
                        {
                            builtInAssets.Add(new BuiltInAssetInfo(line));
                        }
                    }
                    else
                    {
                        // 終端にあるセパレータまでスキップ.
                        if (line.StartsWith("---------------------------------------------"))
                        {
                            collect = true;

                            builtInAssets.Clear();
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            return builtInAssets.OrderByDescending(x => x.size).ToArray();
        }
    }
}
