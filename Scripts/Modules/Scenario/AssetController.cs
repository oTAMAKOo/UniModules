
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Scenario
{
    public sealed class AssetController
    {
        //----- params -----

        //----- field -----

		private List<string> assetRequest = null;

		private List<UniTask> loadQueue = null;

		private Dictionary<string, object> loadedAssets = null;

        //----- property -----

		//----- method -----

		public AssetController()
		{
			assetRequest = new List<string>();
			loadQueue = new List<UniTask>();
			loadedAssets = new Dictionary<string, object>();
		}

		public void AddRequest(string target)
		{
			assetRequest.Add(target);
		}

		public void AddLoadTask(UniTask task)
		{
			loadQueue.Add(task);
		}

		public async UniTask RunLoadTasks()
		{
			if (loadQueue.IsEmpty()){ return; }

			await UniTask.WhenAll(loadQueue);

			loadQueue.Clear();
		}

		public void SetLoadedAsset<T>(string key, T asset)
		{
			loadedAssets[key] = asset;
		}

		public T GetLoadedAsset<T>(string key) where T : class
		{
			return loadedAssets.GetValueOrDefault(key) as T;
		}

		public string[] GetAllRequestAssets()
		{
			return assetRequest.ToArray();
		}
	}
}