
using Cysharp.Threading.Tasks;
using Modules.ExternalResource;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
    public abstract class AssetLoad<T> : ScenarioCommand where T : UnityEngine.Object  
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		public async UniTask LuaCallback(string assetPath, bool? immediate)
		{
			var task = UniTask.Defer(() => LoadAsset(assetPath));

			if (immediate.HasValue && immediate.Value)
			{
				await task;
			}
			else
			{
				scenarioController.AssetController.AddLoadTask(task);
			}
		}

		protected virtual async UniTask LoadAsset(string assetPath)
		{
			var asset = await ExternalResources.LoadAsset<T>(assetPath);

			if (asset != null)
			{
				scenarioController.AssetController.SetLoadedAsset(assetPath, asset);
			}
		}
    }
}