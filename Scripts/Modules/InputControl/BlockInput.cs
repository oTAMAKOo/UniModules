﻿﻿﻿﻿﻿﻿
using UnityEngine;

namespace Modules.InputControl
{
    public sealed class BlockInput : Extensions.Scope
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public ulong BlockingId { get; private set; }

        //----- method -----

        public BlockInput()
        {
            var blockInputManager = BlockInputManager.Instance;

            BlockingId = blockInputManager.GetNextBlockingId();

            blockInputManager.Lock(BlockingId);
        }

        protected override void CloseScope()
        {
            var blockInputManager = BlockInputManager.Instance;

            blockInputManager.Unlock(BlockingId);
        }
    }
}
