﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public abstract partial class UIImage : UIComponent<Image>
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

        void OnEnable()
        {
            #if UNITY_EDITOR

            ApplyDevelopmentAsset();

            #endif
        }

        void OnDisable()
        {
            #if UNITY_EDITOR

            DeleteCreatedAsset();

            #endif
        }
    }
}
