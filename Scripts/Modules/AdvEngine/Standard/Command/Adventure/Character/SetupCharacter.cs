
#if ENABLE_MOONSHARP

using System;
using Modules.AdvKit;
using Modules.PatternTexture;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupCharacter : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetupCharacter"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string, string>)CommandFunction;
        }

        private void CommandFunction(string identifier, string characterName, string fileName)
        {
            var advEngine = AdvEngine.Instance;
            
            var resourcePath = advEngine.Resource.GetResourcePath<AdvCharacter>(fileName);

            advEngine.Resource.Request<PatternTexture.PatternTexture>(resourcePath);

            var advCharacter = advEngine.ObjectManager.Create<AdvCharacter>(identifier);
            
            advCharacter.Setup(characterName, resourcePath);
        }
    }
}

#endif
