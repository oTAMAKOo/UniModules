
#if ENABLE_XLUA

using Modules.Animation;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class StopAnimation : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "StopAnimation"; } }

		public override string Callback 
        {
            get { return BuildCallName<StopAnimation>(nameof(LuaCallback)); }
        }

		//----- method -----

		public void LuaCallback(object target)
		{
			var animationPlayer = ToComponent<AnimationPlayer>(target);

			if (animationPlayer != null)
			{
				animationPlayer.Stop();
			}
		}
	}
}

#endif