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
    [RequireComponent(typeof(RawImage))]
    public abstract class UIRawImage : UIComponent<RawImage>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public RawImage RawImage { get { return component; } }

        public Texture texture
        {
            get { return component.texture; }
            set { component.texture = value; }
        }

        //----- method -----
    }
}
