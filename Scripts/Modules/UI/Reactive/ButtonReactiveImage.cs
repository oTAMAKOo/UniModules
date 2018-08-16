
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Atlas;
using Modules.UI.Element;

namespace Modules.UI.Reactive
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIImage))]
    public class ButtonReactiveImage : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private UIButton target = null;
        [SerializeField]
        private AtlasTexture atlas = null;

        [SerializeField, HideInInspector]
        private string enableSpriteName = null;
        [SerializeField, HideInInspector]
        private string enableSpriteGuid = null;
        [SerializeField, HideInInspector]
        private string disableSpriteName = null;
        [SerializeField, HideInInspector]
        private string disableSpriteGuid = null;

        private UIImage uiImage = null;

        //----- property -----

        public AtlasTexture Atlas { get { return atlas; } }

        public string EnableSpriteName
        {
            get { return enableSpriteName; }

            set
            {
                if (atlas != null && atlas.GetListOfSprites().Contains(value))
                {
                    var spriteData = atlas.GetSpriteData(value);

                    enableSpriteGuid = spriteData.guid;
                    enableSpriteName = value;
                }
                else
                {
                    enableSpriteGuid = null;
                    enableSpriteName = null;
                }

                Apply(target.Button.interactable);
            }
        }

        public string DisableSpriteName
        {
            get { return disableSpriteName; }

            set
            {
                if (atlas != null && atlas.GetListOfSprites().Contains(value))
                {
                    var spriteData = atlas.GetSpriteData(value);

                    disableSpriteGuid = spriteData.guid;
                    disableSpriteName = value;
                }
                else
                {
                    disableSpriteGuid = null;
                    disableSpriteName = null;
                }

                Apply(target.Button.interactable);
            }
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
            var spriteGuid = interactable ? enableSpriteGuid : disableSpriteGuid;

            var spriteData = atlas.GetSpriteData(spriteGuid);

            if (spriteData != null)
            {
                if (interactable)
                {
                    enableSpriteName = spriteData.name;
                }
                else
                {
                    disableSpriteName = spriteData.name;
                }
            }

            var spriteName = interactable ? enableSpriteName : disableSpriteName;

            var sprite = atlas.GetSprite(spriteName); ;

            if (sprite != null)
            {
                uiImage.Image.sprite = sprite;
            }
        }
    }
}