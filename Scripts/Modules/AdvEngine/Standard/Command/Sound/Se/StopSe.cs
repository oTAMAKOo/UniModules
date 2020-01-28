
#if ENABLE_MOONSHARP

using System;

namespace Modules.AdvKit.Standard
{
    public sealed class StopSe : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "StopSe"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string>)CommandFunction;
        }

        private void CommandFunction(string identifier)
        {
            var advEngine = AdvEngine.Instance;

            advEngine.Sound.StopSe(identifier);
        }
    }
}

#endif
