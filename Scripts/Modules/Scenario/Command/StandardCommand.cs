
#if ENABLE_XLUA

using System;

namespace Modules.Scenario.Command
{
	public static class StandardCommand
	{
		public static readonly Type[] CommandTypes = new Type[]
		{
			typeof(AssetRequest),
			typeof(AssetLoadInQueue),

			typeof(TextLoad),
			typeof(GetText),

			typeof(Wait),
			typeof(Show),
			typeof(Hide),

			typeof(CreateObject),
			typeof(DeleteObject),
			typeof(DeleteAllObject),
			typeof(GetObject),
			typeof(SetPriority),

			typeof(Move),
			typeof(MoveX),
			typeof(MoveY),
			typeof(MoveZ),
			typeof(LocalMove),
			typeof(LocalMoveX),
			typeof(LocalMoveY),
			typeof(LocalMoveZ),
			typeof(Rotate),
			typeof(RotateX),
			typeof(RotateY),
			typeof(RotateZ),
			typeof(Scale),
			typeof(ScaleX),
			typeof(ScaleY),
			typeof(ScaleZ),
			typeof(Shake),

			typeof(PlayAnimation),
			typeof(StopAnimation),

			typeof(PlayParticle),
			typeof(StopParticle),

			typeof(FadeIn),
			typeof(FadeOut),
			typeof(FadeColor),

			#if ENABLE_CRIWARE_ADX

			typeof(PlayBgm),
			typeof(PlaySe),
			typeof(PlayVoice),
			typeof(PlayJingle),
			typeof(PlayAmbience),
			typeof(PauseSound),
			typeof(StopSound),
			typeof(StopAllSound),

			#endif
		};
    }
}

#endif