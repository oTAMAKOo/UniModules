
using UnityEngine;
using UnityEngine.U2D;
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.Localize
{
	public abstract class LocalizeAtlasLoader
	{
		public abstract string GetAtlasPath(string atlasFolder);

		public abstract UniTask<SpriteAtlas> Load(string atlasFolder);
	}

    public sealed class LocalizeAtlasManager : Singleton<LocalizeAtlasManager> 
    {
        //----- params -----

		//----- field -----

		private AesCryptoKey cryptoKey = null;

		private LocalizeAtlasLoader atlasLoader = null;

		private LocalizeSpriteAsset localizeSpriteAsset = null;
		
		private Dictionary<string, AtlasCache> atlasCacheByAtlasFolder = null;

		private Subject<Unit> onLoadAtlas = null;

		//----- property -----

		//----- method -----
		
		public void Initialize(AesCryptoKey cryptoKey, LocalizeAtlasLoader atlasLoader, LocalizeSpriteAsset localizeSpriteObject)
		{
			this.cryptoKey = cryptoKey;
			
			atlasCacheByAtlasFolder = new Dictionary<string, AtlasCache>();

			SetAtlasLoader(atlasLoader);
			SetLocalizeSpriteObject(localizeSpriteObject);
		}

		public void SetAtlasLoader(LocalizeAtlasLoader atlasLoader)
		{
			this.atlasLoader = atlasLoader;
		}

		public void SetLocalizeSpriteObject(LocalizeSpriteAsset localizeSpriteAsset)
		{
			this.localizeSpriteAsset = localizeSpriteAsset;

			this.localizeSpriteAsset.SetCryptoKey(cryptoKey);
		}

		public async UniTask LoadAtlas(string atlasFolder, bool force = false)
		{
			if (string.IsNullOrEmpty(atlasFolder)){ return; }

			atlasFolder = PathUtility.ConvertPathSeparator(atlasFolder);

			atlasFolder = atlasFolder.TrimEnd(PathUtility.PathSeparator);

			var atlasCache = atlasCacheByAtlasFolder.GetValueOrDefault(atlasFolder);

			if (!force && atlasCache != null){ return; }

			var atlas = await atlasLoader.Load(atlasFolder);

			if (atlas != null)
			{
				var atlasPath = atlasLoader.GetAtlasPath(atlasFolder);

				atlasCache = new AtlasCache(atlasPath, atlas);

				atlasCacheByAtlasFolder[atlasFolder] = atlasCache;

				if (onLoadAtlas != null)
				{
					onLoadAtlas.OnNext(Unit.Default);
				}
			}
		}

		public void ReleaseAtlas(string atlasFolder)
		{
			if (string.IsNullOrEmpty(atlasFolder)){ return; }

			atlasFolder = PathUtility.ConvertPathSeparator(atlasFolder);

			atlasFolder = atlasFolder.TrimEnd(PathUtility.PathSeparator);
			
			var atlasCache = atlasCacheByAtlasFolder.GetValueOrDefault(atlasFolder);

			if (atlasCache == null){ return; }

			atlasCache.ReleaseReference();

			if (atlasCache.RefCount <= 0)
			{
				atlasCacheByAtlasFolder.Remove(atlasFolder);
			}
		}

		public async UniTask ReloadAllAtlas()
		{
			var atlasFolders = atlasCacheByAtlasFolder.Keys.ToArray();
			
			var tasks = new List<UniTask>();

			foreach (var atlasFolder in atlasFolders)
			{
				var task = UniTask.Defer(() => LoadAtlas(atlasFolder, true));

				tasks.Add(task);
			}

			await UniTask.WhenAll(tasks);
		}

		public void ReleaseAll()
		{
			atlasCacheByAtlasFolder.Clear();
		}

		public string GetFolderPathFromGuid(string folderGuid)
		{
			if (localizeSpriteAsset == null){ return null; }

			return localizeSpriteAsset.GetFolderPath(folderGuid);
		}

		public SpriteAtlas GetSpriteAtlas(string spriteGuid)
		{
			var atlasCache = FindAtlasCache(spriteGuid);

			if (atlasCache == null){ return null; }

			return atlasCache.Atlas;
		}

		public Sprite GetSprite(string spriteGuid, string spriteName)
		{
			var atlasCache = FindAtlasCache(spriteGuid);

			if (atlasCache == null){ return null; }
			
			return atlasCache.GetSprite(spriteName);
		}

		private AtlasCache FindAtlasCache(string spriteGuid)
		{
			// Atlasパス取得.

            var atlasFolder = GetAtlasFolderPath(spriteGuid);

            if (string.IsNullOrEmpty(atlasFolder)){ return null; }

			// AtlasCache取得.

			var atlasCache = atlasCacheByAtlasFolder.GetValueOrDefault(atlasFolder);

			return atlasCache;
		}

        public string GetAtlasFolderPath(string spriteGuid)
        {
            if (localizeSpriteAsset == null){ return null; }

            var atlasFolder = localizeSpriteAsset.GetAtlasFolderPath(spriteGuid);

            return atlasFolder;
        }

		/// <summary> 読み込み済みのフォルダ一覧を取得 </summary>
		public IReadOnlyList<string> GetLoadedFolders()
		{
			return atlasCacheByAtlasFolder.Select(x => x.Key).ToArray();
		}

		public IObservable<Unit> OnLoadAtlasAsObservable()
		{
			return onLoadAtlas ?? (onLoadAtlas = new Subject<Unit>());
		}
	}
}
