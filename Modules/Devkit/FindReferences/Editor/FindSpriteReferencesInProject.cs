
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.FindReferences
{
    public static class FindSpriteReferencesInProject
    {
        private const string MenuItemLabel = "Assets/Find Sprite References In Project";

        private const int MaxWorkerCount = 50;

        [MenuItem(MenuItemLabel, priority = 28)]
        public static void Execute()
        {
            FindSpriteReferenceInternal();
        }

        [MenuItem(MenuItemLabel, validate = true)]
        public static bool CanExecute()
        {
            var target = Selection.activeObject;

            var sprite = target as Sprite;

            return sprite != null;
        }

        private static void FindSpriteReferenceInternal()
        {
            if (Selection.activeObject == null) { return; }

            var target = Selection.activeObject;

            var sprite = target as Sprite;

            if (sprite == null) { return; }

            var texture = sprite.texture;

            var textureAssetPath = AssetDatabase.GetAssetPath(texture);
            var textureMetaFilePath = AssetDatabase.GetAssetPathFromTextMetaFilePath(textureAssetPath);

            if (string.IsNullOrEmpty(textureMetaFilePath)) { return; }

            textureMetaFilePath += ".meta";

            var textureGuid = UnityEditorUtility.GetAssetGUID(sprite.texture);

            var spriteFileId = string.Empty;

            var spriteName = sprite.name;

            using (var sr = new StreamReader(textureMetaFilePath, Encoding.UTF8, false, 256 * 1024))
            {
                var skip = true;

                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line)) { continue; }

                    var str = line.Replace(" ", string.Empty);

                    if (str.StartsWith("fileIDToRecycleName"))
                    {
                        skip = false;
                    }

                    if (skip) { continue; }

                    var findFormat = string.Format(":{0}", spriteName);

                    if (!str.Contains(findFormat)) { continue; }

                    spriteFileId = str.Replace(findFormat, string.Empty);

                    break;
                }
            }

            var assetPath = Application.dataPath;

            var scenes = FindAllFiles(assetPath, "*.unity");
            var prefabs = FindAllFiles(assetPath, "*.prefab");
            var materials = FindAllFiles(assetPath, "*.mat");
            var assets = FindAllFiles(assetPath, "*.asset");

            var scenesReference = new HashSet<string>();
            var prefabsReference = new HashSet<string>();
            var materialsReference = new HashSet<string>();
            var assetsReference = new HashSet<string>();

            Action<int, int> reportProgress = (c, t) =>
            {
                var title = "Find Sprite References In Project";
                var info = string.Format("Find Dependencies ({0}/{1})", c, t);

                EditorUtility.DisplayProgressBar(title, info, c / (float)t);
            };

            var total = scenes.Length + prefabs.Length + materials.Length + assets.Length;

            reportProgress(0, total);

            var progressCounter = new ProgressCounter();

            var events = new WaitHandle[]
            {
                StartResolveReferencesWorker(scenes, spriteFileId, textureGuid, (metadata) => scenesReference = metadata, progressCounter),
                StartResolveReferencesWorker(prefabs, spriteFileId, textureGuid,(metadata) => prefabsReference = metadata, progressCounter),
                StartResolveReferencesWorker(materials, spriteFileId, textureGuid,(metadata) => materialsReference = metadata, progressCounter),
                StartResolveReferencesWorker(assets, spriteFileId, textureGuid,(metadata) => assetsReference = metadata, progressCounter),
            };

            while (!WaitHandle.WaitAll(events, 100))
            {
                reportProgress(progressCounter.Count, total);
            }

            reportProgress(progressCounter.Count, total);

            var builder = new StringBuilder();

            if (scenesReference.Any())
            {
                builder.AppendLine("ScenesReference:");
                scenesReference.ForEach(x => builder.AppendLine(x));
                Debug.Log(builder.ToString());
                builder.Clear();
            }

            if (prefabsReference.Any())
            {
                builder.AppendLine("PrefabsReference:");
                prefabsReference.ForEach(x => builder.AppendLine(x));
                Debug.Log(builder.ToString());
                builder.Clear();
            }

            if (materialsReference.Any())
            {
                builder.AppendLine("MaterialsReference:");
                materialsReference.ForEach(x => builder.AppendLine(x));
                Debug.Log(builder.ToString());
                builder.Clear();
            }

            if (assetsReference.Any())
            {
                builder.AppendLine("AssetsReference:");
                assetsReference.ForEach(x => builder.AppendLine(x));
                Debug.Log(builder.ToString());
                builder.Clear();
            }

            if (scenesReference.IsEmpty() && prefabsReference.IsEmpty() && materialsReference.IsEmpty() && assetsReference.IsEmpty())
            {
                Debug.Log("This asset no reference in project.");
            }

            EditorUtility.ClearProgressBar();
        }

        private static string[] FindAllFiles(string assetPath, string searchPattern)
        {
            return Directory.GetFiles(assetPath, searchPattern, SearchOption.AllDirectories)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();
        }

        private static ManualResetEvent StartResolveReferencesWorker(string[] paths, string spriteFileid, string textureGuid, Action<HashSet<string>> setter, ProgressCounter progressCounter)
        {
            var queue = new Queue<string>(paths);
            var dependencyInfoList = new List<HashSet<string>>();
            var resetEvent = new ManualResetEvent(false);
            var completedCount = 0;

            for (var i = 0; i < MaxWorkerCount; i++)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var dependencyInfoListLocal = new List<HashSet<string>>();

                    while (true)
                    {
                        string path = null;

                        lock (queue)
                        {
                            if (queue.Any()) { path = queue.Dequeue(); }
                        }

                        if (path == null) { break; }

                        var metadata = GetReferencingGuid(path, spriteFileid, textureGuid);

                        dependencyInfoListLocal.Add(metadata);

                        Interlocked.Increment(ref progressCounter.Count);
                    }

                    lock (dependencyInfoList)
                    {
                        dependencyInfoList.AddRange(dependencyInfoListLocal);
                    }

                    // 全部終わった?.
                    if (Interlocked.Increment(ref completedCount) == MaxWorkerCount)
                    {
                        setter(dependencyInfoList.SelectMany(x => x).Distinct().ToHashSet());
                        resetEvent.Set();
                    }
                });
            }

            return resetEvent;
        }

        public static HashSet<string> GetReferencingGuid(string path, string spriteFileid, string textureGuid)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var sr = new StreamReader(path, Encoding.UTF8, false, 256 * 1024))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line)) { continue; }

                    line = line.Replace(" ", string.Empty);

                    var fileIdIndex = line.IndexOf("fileID:", StringComparison.Ordinal);

                    if (fileIdIndex == -1) { continue; }

                    var fileId = "";

                    var startPos = fileIdIndex + 7;

                    var fileIdEndIndex = line.IndexOf(',', startPos);

                    if (0 <= fileIdEndIndex)
                    {
                        fileId = line.SafeSubstring(startPos, fileIdEndIndex - startPos);
                    }

                    if (string.IsNullOrEmpty(fileId)) { continue; }

                    if (spriteFileid != fileId) { continue; }

                    var guidIndex = line.IndexOf("guid:", StringComparison.Ordinal);

                    if (guidIndex == -1) { continue; }

                    var guid = line.SafeSubstring(guidIndex + 5, 32);

                    if (string.IsNullOrEmpty(guid)) { continue; }

                    if (guid != textureGuid) { continue; }

                    if (!result.Contains(path))
                    {
                        result.Add(path);
                    }
                }
            }

            return result;
        }

        private sealed class ProgressCounter
        {
            public int Count;
        }
    }
}
