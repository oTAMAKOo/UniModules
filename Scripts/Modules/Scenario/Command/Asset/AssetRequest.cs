
#if ENABLE_XLUA

using XLua;

namespace Modules.Scenario.Command
{
	/// <summary>
	/// 使用するアセットを要求リストに追加.
	/// </summary>
	[CSharpCallLua]
	public sealed class AssetRequest : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "Asset.Request"; } }

		public override string Callback 
        {
            get { return BuildCallName<AssetRequest>(nameof(LuaCallback)); }
        }

		//----- method -----

		public void LuaCallback(string target)
		{
			scenarioController.AssetController.AddRequest(target);
		}
	}
}

#endif