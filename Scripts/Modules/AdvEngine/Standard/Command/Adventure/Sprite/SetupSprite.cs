
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
            return (Action<string>)CommandFunction;
        }

        private void CommandFunction(string fileName)
        {
            var advEngine = AdvEngine.Instance;

            var resourcePath = advEngine.Resource.GetResourcePath<AdvSprite>(fileName);

            advEngine.Resource.Request<Sprite>(resourcePath);            
        }
    }
}