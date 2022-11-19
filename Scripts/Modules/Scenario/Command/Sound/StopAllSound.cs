
#if ENABLE_CRIWARE_ADX && ENABLE_XLUA

using Modules.Sound;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class StopAllSound : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "StopAllSound"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public void LuaCallback()
		{
			var sounds = scenarioController.SoundController.Elements;

			foreach (var sound in sounds)
			{
				SoundManagement.Instance.Stop(sound);
			}

			scenarioController.SoundController.Clear();
		}
	}
}

#endif
