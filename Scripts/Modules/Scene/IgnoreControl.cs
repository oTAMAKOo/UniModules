﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Scene
{
	public sealed class IgnoreControl : MonoBehaviour
    {
        //----- params -----

        [Flags]
        public enum IgnoreType
        {
            None = 0,

            ActiveControl = 1 << 0x01,
        }

		//----- field -----

		//----- property -----

        public IgnoreType Type { get; set; }

		//----- method -----
	}
}
