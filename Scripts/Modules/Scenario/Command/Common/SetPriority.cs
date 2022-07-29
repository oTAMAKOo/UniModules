
using UnityEngine;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class SetPriority : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "SetPriority"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public void LuaCallback(object target, int priority)
		{
			var component = ToComponent<Component>(target);

			if (component != null)
			{
				component.transform.SetSiblingIndex(priority);
			}
		}
	}
}