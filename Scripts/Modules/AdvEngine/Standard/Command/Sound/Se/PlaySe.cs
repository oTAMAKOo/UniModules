
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class PlaySe : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "PlaySe"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string>)CommandFunction;
        }

        private void CommandFunction(string identifier, string soundIdentifier)
        {
            var advEngine = AdvEngine.Instance;

            advEngine.Sound.PlaySe(identifier, soundIdentifier);
        }
    }
}