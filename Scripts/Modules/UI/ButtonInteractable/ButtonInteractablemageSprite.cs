﻿
using UnityEngine;
using UniRx;
using Extensions;
using Modules.UI.Extension;

namespace Modules.UI.Reactive
{
    [ExecuteAlways]
    [RequireComponent(typeof(UIImage))]
    public sealed class ButtonInteractablemageSprite : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private UIButton target = null;
        [SerializeField]
        private Sprite enableSprite = null;
        [SerializeField]
        private Color enableColor = Color.white;
        [SerializeField]
        private Sprite disableSprite = null;
        [SerializeField]
        private Color disableColor = Color.white;

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
            var color = interactable ? enableColor : disableColor;

            uiImage.Image.sprite = sprite;
            uiImage.color = color;
        }
    }
}
