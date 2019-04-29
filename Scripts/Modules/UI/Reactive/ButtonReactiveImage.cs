
using UnityEngine;
using System.Linq;
using UniRx;
using Extensions;
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
        private SpriteSheet.SpriteSheet spriteSheet = null;

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

        public SpriteSheet.SpriteSheet SpriteSheet { get { return spriteSheet; } }

        public string EnableSpriteName
        {
            get { return enableSpriteName; }

            set
            {
                if (spriteSheet != null && spriteSheet.GetListOfSprites().Contains(value))
                {
                    var spriteData = spriteSheet.GetSpriteData(value);

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
                if (spriteSheet != null && spriteSheet.GetListOfSprites().Contains(value))
                {
                    var spriteData = spriteSheet.GetSpriteData(value);

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

            var spriteData = spriteSheet.GetSpriteData(spriteGuid);

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

            var sprite = spriteSheet.GetSprite(spriteName); ;

            if (sprite != null)
            {
                uiImage.Image.sprite = sprite;
            }
        }
    }
}
