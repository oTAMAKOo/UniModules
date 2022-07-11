
#if ENABLE_MOONSHARP

using System;
using Modules.Sound;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupAmbience : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetupAmbience"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string, string>)CommandFunction;
        }

        private void CommandFunction(string identifier, string acbName, string cueName)
        {
            try
            {
                var advEngine = AdvEngine.Instance;

                var soundInfo = advEngine.Sound.Register(identifier, SoundType.Ambience, acbName, cueName);

                advEngine.Resource.Request(soundInfo.ResourcePath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

    }
}

#endif
