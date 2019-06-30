
#if ENABLE_UTAGE

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Utage;

namespace Modules.UtageExtension
{
    public sealed class AdvExtendCommandFinish : AdvCommand
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public bool IsWait { get; set; }

        //----- method -----

        public AdvExtendCommandFinish(StringGridRow row) : base(row){}

        public override void DoCommand(AdvEngine engine)
        {
            IsWait = true;
        }

        public override bool Wait(AdvEngine engine)
        {
            return IsWait;
        }
    }
}

#endif
