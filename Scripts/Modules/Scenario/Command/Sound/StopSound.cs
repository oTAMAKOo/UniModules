
#if ENABLE_CRIWARE_ADX

using Modules.Sound;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class StopSound : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "StopSound"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public void LuaCallback(SoundElement sound)
		{
			SoundManagement.Instance.Stop(sound);
		}
	}
}

#endif