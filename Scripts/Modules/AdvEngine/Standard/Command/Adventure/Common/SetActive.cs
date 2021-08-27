
#if ENABLE_MOONSHARP

using System;
using Extensions;
using UnityEngine;

namespace Modules.AdvKit.Standard
{
    public sealed class SetActive : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetActive"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, bool>)CommandFunction;
        }

        private void CommandFunction(string identifier, bool state)
        {
            try
            {
                var advEngine = AdvEngine.Instance;

                var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

                if (advObject != null)
                {
                    UnityUtility.SetActive(advObject, state);
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
