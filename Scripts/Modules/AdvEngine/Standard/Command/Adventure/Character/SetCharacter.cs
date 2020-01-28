
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
            return (Action<string, int, Vector2>)CommandFunction;
        }

        private void CommandFunction(string identifier, int face, Vector2 pos)
        {
            var advEngine = AdvEngine.Instance;

            var advCharacter = advEngine.ObjectManager.Get<AdvCharacter>(identifier);

            advCharacter.Show(face);

            advCharacter.transform.localPosition = pos;
        }
    }
}