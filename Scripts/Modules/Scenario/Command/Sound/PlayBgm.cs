
#if ENABLE_CRIWARE_ADX && ENABLE_XLUA

using Cysharp.Threading.Tasks;
using Modules.Sound;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class PlayBgm : PlaySound
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "PlayBgm"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask<SoundElement> LuaCallback(string resourcePath, string cue)
		{
			return await Play(SoundType.Bgm, resourcePath, cue);
		}
	}
}

#endif
