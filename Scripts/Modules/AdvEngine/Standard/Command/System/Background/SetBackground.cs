
#if ENABLE_MOONSHARP

using System;
using Extensions;

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

        private void CommandFunction(string fileIdentifier)
        {
            var advEngine = AdvEngine.Instance;

            var fileName = string.Empty;
            
            if (!string.IsNullOrEmpty(fileIdentifier))
            {
                fileName = advEngine.Resource.FindFileName<AdvBackground>(fileIdentifier);
            }

            var advBackground = advEngine.ObjectManager.Get<AdvBackground>(AdvBackground.UniqueIdentifier);

            if (advBackground != null)
            {
                advBackground.Set(fileName);
            }

            UnityUtility.SetActive(advBackground, !string.IsNullOrEmpty(fileName));
        }
    }
}

#endif
