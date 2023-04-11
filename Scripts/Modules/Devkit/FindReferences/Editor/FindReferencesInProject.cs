
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.FindReferences
{
    public static class FindReferencesInProject
    {
        private const string MenuItemLabel = "Assets/Find References In Project";

        private const int MaxWorkerCount = 50;

        [MenuItem(MenuItemLabel, validate = true)]
        public static bool CanExecute()
        {
            var path = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);

            return Selection.activeObject != null && !path.EndsWith(".unity");
        }

        [MenuItem(MenuItemLabel, priority = 27)]
        public static void Execute()
        {
            var targetAsset = Selection.activeObject;

            if(!AssetDatabase.IsMainAsset(targetAsset)) { return; }

			var title = "Find Dependencies In Project";
			var message = "Search for references in your project?";
			var ok_message = "start";
			var cancel_message = "cancel";

			if (!EditorUtility.DisplayDialog(title, message, ok_message, cancel_message)){ return; }

			var result = Execute(targetAsset);
			
			FindReferencesResultWindow.Open(targetAsset, result);
		}

        public static AssetReferenceInfo Execute(UnityEngine.Object targetObject)
        {
            try
            {
                var assetPath = Application.dataPath;

				void ReportProgress(int current, int total)
				{
					var title = "Find References In Project";
					var info = $"Loading Dependencies ({current}/{total})";
					var progress = current / (float)total;

					EditorUtility.DisplayProgressBar(title, info, progress);
				}

				var scenes = FindAllFiles(assetPath, "*.unity");
                var prefabs = FindAllFiles(assetPath, "*.prefab");
                var materials = FindAllFiles(assetPath, "*.mat");
                var animations = FindAllFiles(assetPath, "*.anim");
                var assets = FindAllFiles(assetPath, "*.asset");

                var ctx = new FindReferenceContext(scenes, prefabs, materials, animations, assets, ReportProgress);

                return FindReferencesCore(ctx, targetObject);
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return null;
        }

        private static string[] FindAllFiles(string assetPath, string searchPattern)
        {
            return Directory.GetFiles(assetPath, searchPattern, SearchOption.AllDirectories)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();
        }

        private static AssetReferenceInfo FindReferencesCore(FindReferenceContext ctx, UnityEngine.Object targetObject)
        {
            try
            {
				var dataPath = Application.dataPath;
				var projectPath = Directory.GetParent(dataPath).FullName;

				projectPath = PathUtility.ConvertPathSeparator(projectPath);

				TargetAssetInfo targetAssetInfo = null;

				var targetObjectPath = AssetDatabase.GetAssetPath(targetObject);
				var targetObjectFullPath = Path.Combine(projectPath, targetObjectPath);

				targetAssetInfo = new TargetAssetInfo(targetObject, targetObjectFullPath.Substring(projectPath.Length + 1), targetObjectFullPath);

				var assets = new List<AssetDependencyInfo>();

				assets.AddRange(ctx.Scenes);
				assets.AddRange(ctx.Prefabs);
				assets.AddRange(ctx.Materials);
				assets.AddRange(ctx.Animations);
				assets.AddRange(ctx.Assets);

				foreach (var asset in assets)
				{
					if (targetAssetInfo.IsReferencedFrom(asset))
					{
						var assetPath = asset.FullPath.Substring(projectPath.Length + 1);

						targetAssetInfo.AssetReferenceInfo.Dependencies.Add(assetPath);
					}
				}

				return targetAssetInfo.AssetReferenceInfo;
			}
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }


        /// <summary>
        /// 依存される側の情報
        /// </summary>
        private sealed class TargetAssetInfo
        {
            //----- params -----

            //----- field -----

            private string guid = null;
            private string fileId = null;

            //----- property -----

            public AssetReferenceInfo AssetReferenceInfo { get; private set; }

            //----- method -----

            internal TargetAssetInfo(Object target, string path, string fullPath)
            {
                this.guid = GetGuid(fullPath);
                this.AssetReferenceInfo = new AssetReferenceInfo(path, target);

                // DLLでMonoScriptだったらDLLの中のコンポーネントなのでfileIDを取り出す.
                if (path.EndsWith(".dll") && target is MonoScript)
                {
                    fileId = UnityEditorUtility.GetLocalIdentifierInFile(target).ToString();
                }
            }

            /// <summary>
            /// 指定したアセットからこのアセットが参照されているかどうか返す.
            /// </summary>
            /// <param name="dependencyInfo"></param>
            /// <returns></returns>
            public bool IsReferencedFrom(AssetDependencyInfo dependencyInfo)
            {
                // fileIDがあるということはDLL.
                if (fileId != null)
                {
                    // DLLの時はGUIDに加えてfileIDも比較.
                    return dependencyInfo.FileIdsByGuid.ContainsKey(guid) && dependencyInfo.FileIdsByGuid[guid].Contains(fileId);
                }
                else
                {
                    return dependencyInfo.FileIdsByGuid.ContainsKey(guid);
                }
            }

            private static string GetGuid(string path)
            {
                using (var sr = new StreamReader(path + ".meta"))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        var index = line.IndexOf("guid:", StringComparison.Ordinal);
                        if (index >= 0)
                            return line.Substring(index + 6, 32);
                    }
                }
                return "0";
            }
        }

        /// <summary>
        /// 参照を持っているアセット(依存を持つ側)の情報.
        /// </summary>
        private sealed class AssetDependencyInfo
        {
            public string FullPath { get; private set; }

            /// <summary>
            /// 参照しているコンポーネントのGUIDとfileIDのセット.
            /// </summary>
            public Dictionary<string, HashSet<string>> FileIdsByGuid { get; private set; }

            public AssetDependencyInfo(string fullPath, Dictionary<string, HashSet<string>> fileIdsByguid)
            {
                this.FullPath = fullPath;
                this.FileIdsByGuid = fileIdsByguid;
            }
        }

        private sealed class FindReferenceContext
        {
            public AssetDependencyInfo[] Scenes { get; private set; }
            public AssetDependencyInfo[] Prefabs { get; private set; }
            public AssetDependencyInfo[] Materials { get; private set; }
            public AssetDependencyInfo[] Animations { get; private set; }
            public AssetDependencyInfo[] Assets { get; private set; }

            public FindReferenceContext(string[] scenes, string[] prefabs, string[] materials, string[] animations, string[] assets, Action<int, int> reportProgress)
            {
                var total = scenes.Length + prefabs.Length + materials.Length + animations.Length + assets.Length;

                reportProgress(0, total);

                // スレッドプールに投げ込んで待つ.
                var progress = new Progress();

                var events = new WaitHandle[]
                {
                    StartResolveReferencesWorker(scenes, (metadata) => Scenes = metadata, progress),
                    StartResolveReferencesWorker(prefabs, (metadata) => Prefabs = metadata, progress),
                    StartResolveReferencesWorker(materials, (metadata) => Materials = metadata, progress),
                    StartResolveReferencesWorker(animations, (metadata) => Animations = metadata, progress),
                    StartResolveReferencesWorker(assets, (metadata) => Assets = metadata, progress),
                };

                while (!WaitHandle.WaitAll(events, 100))
                {
                    reportProgress(progress.Count, total);
                }

                reportProgress(progress.Count, total);
            }

            private sealed class Progress
            {
                public int Count;
            }

            private ManualResetEvent StartResolveReferencesWorker(string[] paths, Action<AssetDependencyInfo[]> setter, Progress progress)
            {
                var queue = new Queue<string>(paths);
                var dependencyInfoList = new List<AssetDependencyInfo>();
                var resetEvent = new ManualResetEvent(false);
                var completedCount = 0;

                for (var i = 0; i < MaxWorkerCount; i++)
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        var dependencyInfoListLocal = new List<AssetDependencyInfo>();

                        while (true)
                        {
                            string path = null;

                            lock (queue)
                            {
                                if (queue.Any()) { path = queue.Dequeue(); }
                            }

                            if (path == null) { break; }

                            var metadata = new AssetDependencyInfo(path, GetReferencingGuid(path));

                            dependencyInfoListLocal.Add(metadata);

                            Interlocked.Increment(ref progress.Count);
                        }
                        
                        lock (dependencyInfoList)
                        {
                            dependencyInfoList.AddRange(dependencyInfoListLocal);
                        }

                        // 全部終わった?.
                        if (Interlocked.Increment(ref completedCount) == MaxWorkerCount)
                        {
                            setter(dependencyInfoList.ToArray());
                            resetEvent.Set();
                        }
                    });
                }

                return resetEvent;
            }
        }

        public static Dictionary<string, HashSet<string>> GetReferencingGuid(string path)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            using (var sr = new StreamReader(path, Encoding.UTF8, false, 256 * 1024))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if(string.IsNullOrEmpty(line)) { continue; }

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

                    var guidIndex = line.IndexOf("guid:", StringComparison.Ordinal);

                    if(guidIndex == -1) { continue; }
                    
                    var guid = line.SafeSubstring(guidIndex + 5, 32);

                    if (string.IsNullOrEmpty(guid)) { continue; }

                    var fileIds = result.GetValueOrDefault(guid);

                    if (fileIds != null)
                    {
                        fileIds.Add(fileId);
                    }
                    else
                    {
                        result.Add(guid, new HashSet<string>(StringComparer.Ordinal) { fileId });
                    }
                }
            }

            return result;
        }
    }
}
