
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RawImage))]
    public abstract partial class UIRawImage : UIComponent<RawImage>
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
