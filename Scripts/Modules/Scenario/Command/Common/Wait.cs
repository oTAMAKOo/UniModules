
using System;
using Cysharp.Threading.Tasks;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class Wait : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "Wait"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback(double value)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(value));
		}
	}
}