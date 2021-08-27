
#if ENABLE_MOONSHARP

using System;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class StopBgm : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "StopBgm"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action)CommandFunction;
        }

        private void CommandFunction()
        {
            try
            {
                var advEngine = AdvEngine.Instance;

                advEngine.Sound.StopBgm();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
