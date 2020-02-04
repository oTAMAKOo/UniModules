
#if ENABLE_MOONSHARP

using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SetSprite : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetSprite"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string, float?, float?>)CommandFunction;
        }

        private void CommandFunction(string identifier, string fileIdentifier, float? width = null, float? height = null)
        {
            var advEngine = AdvEngine.Instance;

            var advSprite = advEngine.ObjectManager.Create<AdvSprite>(identifier);

            var fileName = advEngine.Resource.FindFileName<AdvSprite>(fileIdentifier);

            if (advSprite != null)
            {
                advSprite.Show(fileName, width, height);
            }
        }
    }
}

#endif
