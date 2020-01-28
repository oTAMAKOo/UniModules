
#if ENABLE_MOONSHARP

using System;
using Modules.SoundManagement;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupSe : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetupSe"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string, string>)CommandFunction;
        }

        private void CommandFunction(string identifier, string cueName, string acbName = null)
        {
            var advEngine = AdvEngine.Instance;

            var soundInfo = advEngine.Sound.Register(identifier, SoundType.Se, acbName, cueName);

            advEngine.Resource.Request(soundInfo.ResourcePath);
        }

    }
}

#endif
