
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
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
    }
}
