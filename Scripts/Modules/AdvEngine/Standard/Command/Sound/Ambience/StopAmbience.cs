
#if ENABLE_MOONSHARP

using System;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class StopAmbience : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "StopAmbience"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string>)CommandFunction;
        }

        private void CommandFunction(string identifier)
        {
            try
            {
                var advEngine = AdvEngine.Instance;

                advEngine.Sound.StopAmbience(identifier);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
