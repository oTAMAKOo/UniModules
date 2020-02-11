
#if ENABLE_MOONSHARP

using System;

namespace Modules.AdvKit.Standard
{
    public class SystemCommandController : CommandController
    {
        //----- params -----

        protected static readonly Type[] CommandList = new Type[]
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

#endif
