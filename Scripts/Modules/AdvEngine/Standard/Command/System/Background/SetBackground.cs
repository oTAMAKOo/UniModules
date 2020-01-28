
#if ENABLE_MOONSHARP

using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SetBackground : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetBg"; } }
    
        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string>)CommandFunction;
        }

        private void CommandFunction(string fileName)
        {
            var advEngine = AdvEngine.Instance;

            var advBackground = advEngine.ObjectManager.Get<AdvBackground>(AdvBackground.UniqueIdentifier);

            if (advBackground != null)
            {
                advBackground.Set(fileName);
            }
        }
    }
}

#endif
