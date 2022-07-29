
using Cysharp.Threading.Tasks;
using Modules.Particle;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class PlayParticle : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "PlayParticle"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask LuaCallback(object target, bool? sync)
		{
			var particlePlayer = ToComponent<ParticlePlayer>(target);
			
			if (particlePlayer == null){ return; }

			particlePlayer.SpeedRate = scenarioController.TimeScale.Value;

			if (sync.HasValue && sync.Value)
			{
				await particlePlayer.Play();
			}
			else
			{
				particlePlayer.Play().Forget();
			}
		}
	}
}