
using UnityEngine;
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
        private AtlasTexture.AtlasTexture atlasTexture = null;

        [SerializeField, HideInInspector]
        private string enableSpriteGuid = null;
        [SerializeField, HideInInspector]
        private string disableSpriteGuid = null;

        private UIImage uiImage = null;

        //----- property -----

        public AtlasTexture.AtlasTexture AtlasTexture { get { return atlasTexture; } }

        public string EnableSpriteName
        {
            get
            {
                var spriteData = atlasTexture.GetSpriteDataFromGuid(enableSpriteGuid);

                return spriteData != null ? spriteData.SpriteName : null;
            }

            set
            {
                if (atlasTexture == null) { return; }

                var spriteData = atlasTexture.GetSpriteData(value);

                enableSpriteGuid = spriteData != null ? spriteData.SpriteGuid : null;

                Apply(target.Button.interactable);
            }
        }

        public string DisableSpriteName
        {
            get
            {
                var spriteData = atlasTexture.GetSpriteDataFromGuid(disableSpriteGuid);

                return spriteData != null ? spriteData.SpriteName : null;
            }

            set
            {
                if (atlasTexture == null) { return; }

                var spriteData = atlasTexture.GetSpriteData(value);

                disableSpriteGuid = spriteData != null ? spriteData.SpriteGuid : null;

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

            var spriteData = atlasTexture.GetSpriteDataFromGuid(spriteGuid);
            
            var sprite = atlasTexture.GetSprite(spriteData.SpriteName); ;

            uiImage.Image.sprite = sprite;
        }
    }
}
