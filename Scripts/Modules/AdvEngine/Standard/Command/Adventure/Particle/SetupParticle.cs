
using UnityEngine;
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupParticle : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetupParticle"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string>)CommandFunction;
        }

        private void CommandFunction(string fileName)
        {
            var advEngine = AdvEngine.Instance;
            
            var resourcePath = advEngine.Resource.GetResourcePath<AdvParticle>(fileName);

            advEngine.Resource.Request<GameObject>(resourcePath);
        }
    }
}