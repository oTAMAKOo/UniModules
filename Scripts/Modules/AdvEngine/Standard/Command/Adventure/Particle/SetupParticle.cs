
#if ENABLE_MOONSHARP

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
            return (Action<string, string>)CommandFunction;
        }

        private void CommandFunction(string fileIdentifier, string fileName)
        {
            try
            {
                var advEngine = AdvEngine.Instance;

                advEngine.Resource.RegisterFileName<AdvParticle>(fileIdentifier, fileName);

                var resourcePath = advEngine.Resource.GetResourcePath<AdvParticle>(fileName);

                advEngine.Resource.Request<GameObject>(resourcePath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
