
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class StopVoice : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "StopVoice"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string>)CommandFunction;
        }

        private void CommandFunction(string identifier)
        {
            var advEngine = AdvEngine.Instance;

            advEngine.Sound.StopVoice(identifier);
        }
    }
}