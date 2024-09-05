
#if ENABLE_XLUA

using UnityEngine;
using XLua;
using Extensions;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class CreateObject : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "CreateObject"; } }

		public override string Callback 
        {
            get { return BuildCallName<CreateObject>(nameof(LuaCallback)); }
        }

		//----- method -----

		public GameObject LuaCallback(string name, GameObject parent, string assetPath)
		{
			var prefab = scenarioController.AssetController.GetLoadedAsset<GameObject>(assetPath);

			if (prefab == null){ return null; }

			var gameObject = UnityUtility.Instantiate(parent, prefab);
			
			scenarioController.ManagedObjects.Add(name, gameObject);

			return gameObject;
		}
	}
}

#endif