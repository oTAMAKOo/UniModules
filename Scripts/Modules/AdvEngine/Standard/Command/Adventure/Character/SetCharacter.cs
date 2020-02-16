
#if ENABLE_MOONSHARP

using UnityEngine;
using System;

namespace Modules.AdvKit.Standard
{
    public sealed class SetCharacter : Command
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "SetCharacter"; } }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Action<string, string, Vector2, int?>)CommandFunction;
        }

        private void CommandFunction(string identifier, string patternName, Vector2 pos, int? priority = null)
        {
            var advEngine = AdvEngine.Instance;

            var advCharacter = advEngine.ObjectManager.Get<AdvCharacter>(identifier);

            if (advCharacter != null)
            {
                advCharacter.SetPriority(priority.HasValue ? priority.Value : 0);

                advCharacter.Show(patternName);

                advCharacter.transform.localPosition = pos;
            }
        }
    }
}

#endif
