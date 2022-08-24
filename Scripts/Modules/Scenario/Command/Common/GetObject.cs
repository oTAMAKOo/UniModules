
using UnityEngine;
using XLua;
using Extensions;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class GetObject : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "GetObject"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public object LuaCallback(string key)
		{
			return scenarioController.ManagedObjects.Get(key);
		}
	}
}