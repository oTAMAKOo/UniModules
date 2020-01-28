
#if ENABLE_MOONSHARP

using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SetPriority : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetPriority"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, int>)CommandFunction;
        }

        private void CommandFunction(string identifier, int priority)
        {
            var advEngine = AdvEngine.Instance;

            var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

            if (advObject != null)
            {
                advObject.SetPriority(priority);
            }
        }
    }
}

#endif
