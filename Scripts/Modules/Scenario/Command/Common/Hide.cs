﻿
#if ENABLE_XLUA

using UnityEngine;
using Extensions;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class Hide : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "Hide"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public void LuaCallback(object target)
		{
			var component = ToComponent<Component>(target);

			UnityUtility.SetActive(component, false);
		}
	}
}

#endif