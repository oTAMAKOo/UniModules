
#if ENABLE_MOONSHARP

using System;
using Modules.Sound;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupBgm : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetupBgm"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string, string>)CommandFunction;
        }

        private void CommandFunction(string soundIdentifier, string acbName, string cueName = null)
        {
            try
            {
                var advEngine = AdvEngine.Instance;

                if (string.IsNullOrEmpty(cueName))
                {
                    cueName = acbName;
                }

                var soundInfo = advEngine.Sound.Register(soundIdentifier, SoundType.Bgm, acbName, cueName);

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
