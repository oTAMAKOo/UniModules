
#if ENABLE_MOONSHARP

using System;
using Extensions;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class Delete : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "Delete"; } }

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

                advEngine.ObjectManager.Delete(identifier);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
