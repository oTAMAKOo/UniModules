
#if ENABLE_MOONSHARP

using System;
using Extensions;

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
            var advEngine = AdvEngine.Instance;

            var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

            if (advObject != null)
            {
                UnityUtility.SafeDelete(advObject);
            }
        }
    }
}

#endif
