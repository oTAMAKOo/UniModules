
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
		public abstract string GetAtlasPath(string altasFolder);

		public abstract UniTask<SpriteAtlas> Load(string altasFolder);
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

		public async UniTask LoadAtlas(string altasFolder, bool force = false)
		{
			if (string.IsNullOrEmpty(altasFolder)){ return; }

			altasFolder = PathUtility.ConvertPathSeparator(altasFolder);

			altasFolder = altasFolder.TrimEnd(PathUtility.PathSeparator);

			var atlasCache = atlasCacheByAtlasFolder.GetValueOrDefault(altasFolder);

			if (!force && atlasCache != null){ return; }

			var atlas = await atlasLoader.Load(altasFolder);

			if (atlas != null)
			{
				var atlasPath = atlasLoader.GetAtlasPath(altasFolder);

				atlasCache = new AtlasCache(atlasPath, atlas);

				atlasCacheByAtlasFolder[altasFolder] = atlasCache;

				if (onLoadAtlas != null)
				{
					onLoadAtlas.OnNext(Unit.Default);
				}
			}
		}

		public void ReleaseAtlas(string altasFolder)
		{
			if (string.IsNullOrEmpty(altasFolder)){ return; }

			altasFolder = PathUtility.ConvertPathSeparator(altasFolder);

			altasFolder = altasFolder.TrimEnd(PathUtility.PathSeparator);
			
			var atlasCache = atlasCacheByAtlasFolder.GetValueOrDefault(altasFolder);

			if (atlasCache == null){ return; }

			atlasCache.ReleaseReference();

			if (atlasCache.RefCount <= 0)
			{
				atlasCacheByAtlasFolder.Remove(altasFolder);
			}
		}

		public async UniTask ReloadAllAtlas()
		{
			var atlasFolders = atlasCacheByAtlasFolder.Keys.ToArray();
			
			var tasks = new List<UniTask>();

			foreach (var altasFolder in atlasFolders)
			{
				var task = UniTask.Defer(() => LoadAtlas(altasFolder, true));

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

			if (localizeSpriteAsset == null){ return null; }

			var altasFolder = localizeSpriteAsset.GetAtlasFolderPath(spriteGuid);

			// AtlasCache取得.

			var atlasCache = atlasCacheByAtlasFolder.GetValueOrDefault(altasFolder);

			if (atlasCache == null)
			{
				// 読み込みされていないのでエラーを出す.
				Debug.LogErrorFormat("Atlas not loaded. Atlas pre load required.\nFolder : {0}", altasFolder);
			}

			return atlasCache;
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