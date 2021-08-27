
#if ENABLE_MOONSHARP

using UnityEngine;
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SetupBackground : Command
    {
        //----- params -----

        //----- field -----

        private AdvBackground advBackground = null;

        //----- property -----

        public override string CommandName { get { return "SetupBg"; } }

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

                advEngine.Resource.RegisterFileName<AdvBackground>(fileIdentifier, fileName);

                var resourcePath = advEngine.Resource.GetResourcePath<AdvBackground>(fileName);

                advEngine.Resource.Request<Sprite>(resourcePath);

                if (advBackground == null)
                {
                    advBackground = advEngine.ObjectManager.Create<AdvBackground>(AdvBackground.UniqueIdentifier);

                    advBackground.transform.SetAsFirstSibling();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}

#endif
