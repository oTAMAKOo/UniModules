
using UnityEngine;
using UnityEngine.UI;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class FadeColor : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "FadeColor"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		public Graphic TargetGraphic { get; set; }

		//----- method -----

		public void LuaCallback(Color color)
		{
			TargetGraphic.color = color;
		}
	}
}