
#if ENABLE_MOONSHARP

using System;
using Extensions;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class ResetTransform : Command
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public override string CommandName { get { return "ResetTransform"; } }

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

                var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

                if (advObject != null)
                {
                    advObject.transform.Reset();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
