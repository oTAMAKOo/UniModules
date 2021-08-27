
#if ENABLE_MOONSHARP

using System;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class PlayAmbience : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "PlayAmbience"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string>)CommandFunction;
        }

        private void CommandFunction(string identifier, string soundIdentifier)
        {
            try
            {
                var advEngine = AdvEngine.Instance;

                advEngine.Sound.PlayAmbience(identifier, soundIdentifier);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
