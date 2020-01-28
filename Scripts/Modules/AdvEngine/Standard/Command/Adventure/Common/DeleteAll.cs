
#if ENABLE_MOONSHARP

using System;

namespace Modules.AdvKit.Standard
{
    public sealed class DeleteAll : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "DeleteAll"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action)CommandFunction;
        }

        private void CommandFunction()
        {
            var advEngine = AdvEngine.Instance;

            advEngine.ObjectManager.DeleteAll();
        }
    }
}

#endif
