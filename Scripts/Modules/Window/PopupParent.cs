﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Window
{
    public sealed class PopupParent : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Canvas canvas = null;
        [SerializeField]
        private GameObject parent = null;

        //----- property -----

        public Canvas Canvas { get { return canvas; } }
        public GameObject Parent { get { return parent; } }

        //----- method -----
    }
}
