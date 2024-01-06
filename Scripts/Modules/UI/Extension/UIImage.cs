
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

        #pragma warning disable 0414

        // 開発アセット登録用.

        [SerializeField, HideInInspector]
        private string assetGuid = null;
        [SerializeField, HideInInspector]
        private string spriteId = null;

        #pragma warning restore 0414

        //----- property -----

        public Image Image { get { return component; } }

        public Sprite sprite
        {
            get { return component.sprite; }
            set { component.sprite = value; }
        }

		public Color color
		{
			get { return Image.color; }

			set { Image.color = value; }
		}

		public float alpha
		{
			get { return Image.color.a; }

			set
			{
				var color = Image.color;

				color.a = value;

				Image.color = color;
			}
		}

        //----- method -----

        void OnEnable()
        {
            #if UNITY_EDITOR

            ApplyDummyAsset();

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
