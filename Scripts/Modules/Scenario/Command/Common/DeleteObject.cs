
using UnityEngine;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class DeleteObject : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "DeleteAll"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public void LuaCallback(object target)
		{
			var component = ToComponent<Component>(target);

			UnityUtility.SafeDelete(component);

			scenarioController.ManagedObjects.Remove(target);
		}
	}
}