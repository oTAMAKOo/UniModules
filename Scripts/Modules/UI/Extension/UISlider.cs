﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Slider))]
    public abstract class UISlider : UIComponent<Slider>
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

        protected override void Modify(){}
    }
}
