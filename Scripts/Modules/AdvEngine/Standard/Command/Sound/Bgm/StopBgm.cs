
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class StopBgm : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "StopBgm"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action)CommandFunction;
        }

        private void CommandFunction()
        {
            var advEngine = AdvEngine.Instance;

            advEngine.Sound.StopBgm();
        }
    }
}