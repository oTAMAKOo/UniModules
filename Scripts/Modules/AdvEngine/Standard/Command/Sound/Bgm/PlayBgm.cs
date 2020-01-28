
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class PlayBgm : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "PlayBgm"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string>)CommandFunction;
        }

        private void CommandFunction(string soundIdentifier)
        {
            var advEngine = AdvEngine.Instance;

            advEngine.Sound.PlayBgm(soundIdentifier);
        }
    }
}