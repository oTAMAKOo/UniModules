﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Slider))]
    public abstract class UISlider : UIElement<Slider>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Slider Slider { get { return component; } }

        public float value
        {
            get { return component.value; }
            set { component.value = value; }
        }

        //----- method -----

        public override void Modify()
        {

        }
    }
}