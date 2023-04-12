
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Devkit.FindReferences
{
    public static class FindReferencesInProject
    {
		private const string MenuItemLabel = "Assets/Find References In Project";

		private const string ProgressBarTitle = "Find References In Project";

		private static readonly Dictionary<string, string> SearchPatterns = new Dictionary<string, string>()
		{
			{ "scene", "*.unity" },
			{ "prefab", "*.prefab" },
			{ "material", "*.mat" },
			{ "animation", "*.anim" },
			{ "timeline", "*.playable" },
			{ "model", "*.fbx" },
			{ "asset", "*.asset" },
		};

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

			FindAssetReferences(targetAsset).Forget();
		}

        public static async UniTask FindAssetReferences(UnityEngine.Object targetAsset)
        {
			try
			{
				//------ 対象ファイル一覧取得 ------

				var assetPath = Application.dataPath;

				var allFiles = new List<string>();

				for (var i = 0; i < SearchPatterns.Count; i++)
				{
					var item = SearchPatterns.ElementAt(i);

					EditorUtility.DisplayProgressBar(ProgressBarTitle, $"Search {item.Key}", (float)i / SearchPatterns.Count);

					var files = FindAllFiles(assetPath, item.Value);

					allFiles.AddRange(files);
				}

				//------ 参照検索 ------

				var cache = new FindReferencesInProjectCache();

				EditorUtility.DisplayProgressBar(ProgressBarTitle, "Load cache data", 0);

				await cache.Load();

				EditorUtility.DisplayProgressBar(ProgressBarTitle, "Load cache data", 1);

				var searchResult = await SearchReferences(allFiles.ToArray(), cache);

				await cache.Save();

				//------ 参照情報構築 ------

				var assetReferenceInfo = BuildAssetReferenceInfo(searchResult, targetAsset);

				FindReferencesResultWindow.Open(targetAsset, assetReferenceInfo);
			}
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
		}

        private static string[] FindAllFiles(string assetPath, string searchPattern)
        {
            return Directory.GetFiles(assetPath, searchPattern, SearchOption.AllDirectories)
				.Where(x => !string.IsNullOrEmpty(x))
				.Select(x => PathUtility.ConvertPathSeparator(x))
				.ToArray();
        }

		public static async UniTask<AssetDependencyInfo[]> SearchReferences(string[] paths, FindReferencesInProjectCache cache)
		{
			var dependencies = new List<AssetDependencyInfo>();

			var count = 0;
			var totalCount = paths.Length;

			var tasks = new List<UniTask>();

			for (var i = 0; i < totalCount; i++)
			{
				var path = paths[i];

				var task = UniTask.RunOnThreadPool(() =>
				{
					var lastUpdate = File.GetLastWriteTimeUtc(path);

					var metadata = cache.GetCache(path, lastUpdate);

					if (metadata == null)
					{
						var referencingGuid = GetReferencingGuid(path);

						metadata = new AssetDependencyInfo(path, referencingGuid);

						cache.Update(metadata, lastUpdate);
					}

					lock (dependencies)
					{
						dependencies.Add(metadata);
					}

					Interlocked.Increment(ref count);
				});

				tasks.Add(task);
			}

			async UniTask DisplayProgress()
			{
				while (true)
				{
					EditorUtility.DisplayProgressBar(ProgressBarTitle, $"Search references ({count}/{totalCount})", (float)count / totalCount);

					if (totalCount <= count){ break; }

					await UniTask.DelayFrame(5);
				}

				EditorUtility.ClearProgressBar();
			}
			
			try
			{
				DisplayProgress().Forget();

				await UniTask.WhenAll(tasks);
			}
			finally
			{
				totalCount = -1;

				await UniTask.SwitchToMainThread();
			}

			return dependencies.ToArray();
		}

		private static AssetReferenceInfo BuildAssetReferenceInfo(AssetDependencyInfo[] dependencies, UnityEngine.Object targetObject)
		{
			var dataPath = Application.dataPath;
			var projectPath = Directory.GetParent(dataPath).FullName;

			projectPath = PathUtility.ConvertPathSeparator(projectPath);
			
			var targetObjectPath = AssetDatabase.GetAssetPath(targetObject);
			var targetObjectFullPath = Path.Combine(projectPath, targetObjectPath);

			var targetAssetInfo = new TargetAssetInfo(targetObject, targetObjectFullPath.Substring(projectPath.Length + 1), targetObjectFullPath);
			
			var totalCount = dependencies.Length;

			for (var i = 0; i < totalCount; i++)
			{
				var dependencyInfo = dependencies[i];

				EditorUtility.DisplayProgressBar(ProgressBarTitle, $"Check reference {dependencyInfo.FullPath}", (float)i / totalCount);

				if (targetAssetInfo.IsReferencedFrom(dependencyInfo))
				{
					var assetPath = dependencyInfo.FullPath.Substring(projectPath.Length + 1);

					targetAssetInfo.AssetReferenceInfo.Dependencies.Add(assetPath);
				}
			}

			return targetAssetInfo.AssetReferenceInfo;
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
