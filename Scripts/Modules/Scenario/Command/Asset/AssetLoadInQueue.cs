
using Cysharp.Threading.Tasks;
using XLua;

namespace Modules.Scenario.Command
{
	/// <summary>
	/// 読み込みキューに入っている読み込みを実行.
	/// </summary>
	[CSharpCallLua]
	public sealed class AssetLoadInQueue : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "Asset.LoadInQueue"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback()
		{
			await scenarioController.AssetController.RunLoadTasks();
		}
	}
}