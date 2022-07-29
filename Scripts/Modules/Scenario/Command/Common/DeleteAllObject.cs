
using UnityEngine;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class DeleteAllObject : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "DeleteAll"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public void LuaCallback()
		{
			var targets = scenarioController.ManagedObjects.GetAll();

			foreach (var target in targets)
			{
				var component = ToComponent<Component>(target);

				UnityUtility.SafeDelete(component);
			}

			scenarioController.ManagedObjects.Clear();
		}
	}
}