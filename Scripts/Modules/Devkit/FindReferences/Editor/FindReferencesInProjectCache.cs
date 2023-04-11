
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Resolvers;
using Extensions;
using Modules.MessagePack;

namespace Modules.Devkit.FindReferences
{
    public sealed class FindReferencesInProjectCache
	{
		//----- params -----

		private const string CacheFileName = "FindReferencesInProject.cache";

		[Serializable]
		[MessagePackObject(true)]
		public sealed class CacheData
		{
			public string FullPath { get; private set; }

			public Dictionary<string, string[]> Dependencies { get; private set; }

			public DateTime LastUpdate { get; private set; }

			[SerializationConstructor]
			public CacheData(string fullPath, Dictionary<string, string[]> dependencies, DateTime lastUpdate)
			{
				FullPath = fullPath;
				Dependencies = dependencies;
				LastUpdate = lastUpdate;
			}
		}

		[MessagePackObject(true)]
		public sealed class CacheContainer
		{
			public CacheData[] contents = null;
		}

		//----- field -----

		private Dictionary<string, CacheData> cache = null;

		private bool requireCacheFileUpdate = false;

		//----- property -----

		//----- method -----

		public FindReferencesInProjectCache()
		{
			cache = new Dictionary<string, CacheData>();
		}

		public bool HasCache()
		{
			var filePath = GetCacheFilePath();

			return File.Exists(filePath);
		}

		public void Load()
		{
			if (!HasCache()) { return; }

			var filePath = GetCacheFilePath();

			using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				var bytes = new byte[fileStream.Length];

				fileStream.Read(bytes, 0, bytes.Length);

				var options = StandardResolverAllowPrivate.Options
					.WithResolver(UnityCustomResolver.Instance);

				var container = MessagePackSerializer.Deserialize<CacheContainer>(bytes, options);

				cache = container.contents.ToDictionary(x => x.FullPath);
			}
		}

		public void Save()
		{
			if (!requireCacheFileUpdate){ return; }

			var container = new CacheContainer()
			{
				contents = cache.Values.ToArray()
			};

			var filePath = GetCacheFilePath();

			using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
			{
				var options = StandardResolverAllowPrivate.Options
					.WithResolver(UnityCustomResolver.Instance);

				var bytes = MessagePackSerializer.Serialize(container, options);

				fileStream.Write(bytes, 0, bytes.Length);
			}
		}

		public void Update(AssetDependencyInfo info, DateTime lastUpdate)
		{
			if (info == null) { return; }

			lock (cache)
			{
				var dependencies = new Dictionary<string, string[]>();

				foreach (var item in info.FileIdsByGuid)
				{
					dependencies.Add(item.Key, item.Value.ToArray());
				}

				cache[info.FullPath] = new CacheData(info.FullPath, dependencies, lastUpdate);
			}

			requireCacheFileUpdate = true;
		}

		public AssetDependencyInfo GetCache(string path, DateTime lastUpdate)
		{
			var cacheData = cache.GetValueOrDefault(path);

			if (cacheData == null){ return null; }

			// キャッシュされたデータより新しい.

			if (cacheData.LastUpdate < lastUpdate){ return null; }

			// キャッシュからデータ作成.

			var fileIdsByGuid = new Dictionary<string, HashSet<string>>();

			foreach (var dependencies in cacheData.Dependencies)
			{
				fileIdsByGuid.Add(dependencies.Key, dependencies.Value.ToHashSet());
			}

			return new AssetDependencyInfo(cacheData.FullPath, fileIdsByGuid);
		}

		private string GetCacheFilePath()
		{
			var projectFolderPath = UnityPathUtility.GetProjectFolderPath();
			
			return PathUtility.Combine(projectFolderPath, UnityPathUtility.LibraryFolder, CacheFileName);
		}
	}
}