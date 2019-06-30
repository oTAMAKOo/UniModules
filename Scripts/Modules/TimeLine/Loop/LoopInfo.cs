
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.TimeLine
{
	public sealed class LoopInfo
	{
        //----- params -----

        //----- field -----

        //----- property -----

        public string Label { get; private set; }

        public bool Loop { get; set; }

		//----- method -----

        public LoopInfo(string label)
        {
            Label = label;
        }
    }
}
