
#if ENABLE_XLUA

using Modules.Particle;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class StopParticle : ScenarioCommand
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "StopParticle"; } }

        public override string Callback 
        {
            get { return BuildCallName<StopParticle>(nameof(LuaCallback)); }
        }

		//----- method -----

		public void LuaCallback(object target)
		{
			var particlePlayer = ToComponent<ParticlePlayer>(target);

			if (particlePlayer != null)
			{
				particlePlayer.Stop();
			}
		}
	}
}

#endif
