﻿
#if ENABLE_MOONSHARP

using System;
using UnityEngine;

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
            try
            {
                var advEngine = AdvEngine.Instance;

                advEngine.Sound.PlayVoice(identifier, soundIdentifier);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
