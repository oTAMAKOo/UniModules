
using System;
using Modules.Scenario.Command;

namespace Modules.Lua.Command
{
	public abstract partial class CommandLoader
	{
		protected static readonly Type[] CommandTypes = new Type[]
		{
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
				
			typeof(AssetRequest),
			typeof(AssetLoadInQueue),

			typeof(PlayAnimation),
			typeof(StopAnimation),

			typeof(PlayParticle),
			typeof(StopParticle),

			typeof(FadeIn),
			typeof(FadeOut),
			typeof(FadeColor),

			typeof(PlayBgm),
			typeof(PlaySe),
			typeof(PlayVoice),
			typeof(PlayJingle),
			typeof(PlayAmbience),
			typeof(PauseSound),
			typeof(StopSound),
			typeof(StopAllSound),

			typeof(Message),
			typeof(Talk),
		};
    }
}