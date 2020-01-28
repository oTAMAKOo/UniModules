
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SystemCommandController : CommandController
    {
        //----- params -----

        private static readonly Type[] CommandList = new Type[]
        {
            typeof(LoadRequest),
            typeof(Wait),
            typeof(SetScreenCover),
            typeof(ScreenFadeIn),
            typeof(ScreenFadeOut),
        };

        //----- field -----

        //----- property -----

        public override string LuaName { get { return "system"; } }

        protected override Type[] CommandTypes { get { return CommandList; } }

        //----- method -----        
    }
}