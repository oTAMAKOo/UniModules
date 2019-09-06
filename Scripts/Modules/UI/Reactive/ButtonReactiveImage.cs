
using UnityEngine;
using UniRx;
using Extensions;
using Modules.UI.Extension;

namespace Modules.UI.Reactive
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIImage))]
    public sealed class ButtonReactiveImage : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private UIButton target = null;
        [SerializeField]
        private Sprite enableSprite = null;
        [SerializeField]
        private Sprite disableSprite = null;

        private UIImage uiImage = null;

        //----- property -----

        public Sprite EnableSprite
        {
            get { return enableSprite; }
            set { enableSprite = value; }
        }

        public Sprite DisableSpriteName
        {
            get { return disableSprite; }
            set { disableSprite = value; }
        }

        //----- method -----

        void Awake()
        {
            uiImage = UnityUtility.GetComponent<UIImage>(gameObject);

            if (target != null && Application.isPlaying)
            {
                target.ObserveEveryValueChanged(x => x.Button.interactable).Subscribe(x => Apply(x)).AddTo(this);
            }
        }

        void OnEnable()
        {
            if (target != null)
            {
                Apply(target.Button.interactable);
            }
        }

        private void Apply(bool interactable)
        {
            var sprite = interactable ? enableSprite : disableSprite;

            uiImage.Image.sprite = sprite;
        }
    }
}
