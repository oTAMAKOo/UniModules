
#if ENABLE_CRIWARE_ADX && ENABLE_XLUA

using Cysharp.Threading.Tasks;
using Modules.Sound;
using XLua;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
	public sealed class PlaySe : PlaySound
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public override string LuaName { get { return "PlaySe"; } }

		public override string Callback { get { return nameof(LuaCallback); } }

		//----- method -----

		public async UniTask<SoundElement> LuaCallback(string resourcePath, string cue)
		{
			return await Play(SoundType.Se, resourcePath, cue);
		}
	}
}

#endif
