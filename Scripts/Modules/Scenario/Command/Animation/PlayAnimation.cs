
#if ENABLE_XLUA

using Cysharp.Threading.Tasks;
using Modules.Animation;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class PlayAnimation : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "PlayAnimation"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback(object target, string animation, bool? sync)
		{
			var animationPlayer = ToComponent<AnimationPlayer>(target);

			if (animationPlayer == null){ return; }

			animationPlayer.SpeedRate = scenarioController.TimeScale.Value;

			if (sync.HasValue && sync.Value)
			{
				await animationPlayer.Play(animation);
			}
			else
			{
				animationPlayer.Play(animation).Forget();
			}
		}
	}
}

#endif