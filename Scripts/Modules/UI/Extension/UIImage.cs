﻿﻿﻿﻿
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
    [RequireComponent(typeof(Image))]
    public abstract class UIImage : UIComponent<Image>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Image Image { get { return component; } }

        public Sprite sprite
        {
            get { return component.sprite; }
            set { component.sprite = value; }
        }

        //----- method -----

        protected override void Modify()
        {

        }
    }
}
