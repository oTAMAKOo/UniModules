
#if ENABLE_MOONSHARP

using UnityEngine;
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupSprite : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetupSprite"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string>)CommandFunction;
        }

        private void CommandFunction(string fileIdentifier, string fileName)
        {
            var advEngine = AdvEngine.Instance;

            advEngine.Resource.RegisterFileName<AdvSprite>(fileIdentifier, fileName);

            var resourcePath = advEngine.Resource.GetResourcePath<AdvSprite>(fileName);

            advEngine.Resource.Request<Sprite>(resourcePath);            
        }
    }
}

#endif
