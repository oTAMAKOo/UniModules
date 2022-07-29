
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class GetText : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "Text"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public string LuaCallback(string id)
		{
			return scenarioController.LuaText.Get(id);
		}
	}
}