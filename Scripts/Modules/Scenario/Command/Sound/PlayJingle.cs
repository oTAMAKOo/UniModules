
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_XLUA

using Cysharp.Threading.Tasks;
using Modules.Sound;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class PlayJingle : PlaySound
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "PlayJingle"; } }

        public override string Callback 
        {
            get { return BuildCallName<PlayJingle>(nameof(LuaCallback)); }
        }

		//----- method -----

		public async UniTask<SoundElement> LuaCallback(string resourcePath, string cue)
		{
			return await Play(SoundType.Jingle, resourcePath, cue);
		}
	}
}

#endif
