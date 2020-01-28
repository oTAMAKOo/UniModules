
#if ENABLE_MOONSHARP

using System;

namespace Modules.AdvKit.Standard
{
    public sealed class PlayVoice : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "PlayVoice"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string>)CommandFunction;
        }

        private void CommandFunction(string identifier, string soundIdentifier)
        {
            var advEngine = AdvEngine.Instance;

            advEngine.Sound.PlayVoice(identifier, soundIdentifier);
        }
    }
}

#endif
