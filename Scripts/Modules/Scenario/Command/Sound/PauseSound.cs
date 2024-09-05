
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_XLUA

using Modules.Sound;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class PauseSound : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "PauseSound"; } }

        public override string Callback 
        {
            get { return BuildCallName<PauseSound>(nameof(LuaCallback)); }
        }

		//----- method -----

		public void LuaCallback(SoundElement sound)
		{
			SoundManagement.Instance.Pause(sound);
		}
	}
}

#endif
