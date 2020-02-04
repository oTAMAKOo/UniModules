
#if ENABLE_MOONSHARP

using System;
using Modules.SoundManagement;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupVoice : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetupVoice"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string, string>)CommandFunction;
        }

        private void CommandFunction(string soundIdentifier, string acbName, string cueName)
        {
            var advEngine = AdvEngine.Instance;

            var soundInfo = advEngine.Sound.Register(soundIdentifier, SoundType.Voice, acbName, cueName);

            advEngine.Resource.Request(soundInfo.ResourcePath);
        }

    }
}

#endif
