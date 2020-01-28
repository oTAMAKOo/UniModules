
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class AdvCommandController : CommandController
    {
        //----- params -----

        private static readonly Type[] CommandList = new Type[]
        {
            //====== Common ======

            typeof(SetActive),
            typeof(Delete), typeof(DeleteAll),
            typeof(SetPriority),

            typeof(PlayAnimation),
            typeof(PlayParticle),

            typeof(Show), typeof(Hide),

            //====== Transform ======

            typeof(ResetTransform),

            typeof(Move), typeof(MoveX), typeof(MoveY), typeof(MoveZ),
            typeof(Rotate), typeof(RotateX), typeof(RotateY), typeof(RotateZ),
            typeof(Scale), typeof(ScaleX), typeof(ScaleY), typeof(ScaleZ),

            typeof(Shake),

            //====== Cover ======

            typeof(SetCover),
            typeof(FadeIn), typeof(FadeOut),

            //====== Message ======

            typeof(Message), typeof(Talk),

            //====== Background ======

            typeof(SetupBackground),
            typeof(SetBackground),

            //====== Sprite ======

            typeof(SetupSprite),
            typeof(SetSprite),

            //====== Character ======

            typeof(SetupCharacter),
            typeof(SetCharacter),
            typeof(SetFace),
        };

        //----- field -----

        //----- property -----

        public override string LuaName { get { return "adv"; } }

        protected override Type[] CommandTypes { get { return CommandList; } }

        //----- method -----
    }
}