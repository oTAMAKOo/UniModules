
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.AtlasTexture
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class AtlasTextureImage : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private AtlasTexture atlasTexture = null;
        [SerializeField]
        private string spriteGuid = null;

        private Image targetImage = null;

        //----- property -----

        public AtlasTexture AtlasTexture { get { return atlasTexture; } }

        public string SpriteName
        {
            get
            {
                if (atlasTexture == null) { return null; }

                var spriteData = atlasTexture.GetSpriteDataFromGuid(spriteGuid);

                return spriteData != null ? spriteData.SpriteName : null;
            }

            set
            {
                if (atlasTexture == null) { return; }

                var spriteData = atlasTexture.GetSpriteData(value);

                if (spriteData != null)
                {
                    spriteGuid = spriteData.SpriteGuid;

                    Apply();
                }
                else
                {
                    Empty();
                }
            }
        }

        public Sprite Sprite { get; private set; }

        //----- method -----

        void Start()
        {
            if (targetImage == null)
            {
                targetImage = UnityUtility.GetComponent<Image>(gameObject);
            }

            // 既に画像が設定されていた場合処理しない.
            if (targetImage.sprite == null)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (targetImage == null)
            {
                targetImage = UnityUtility.GetComponent<Image>(gameObject);
            }

            if (atlasTexture == null) { return; }

            Sprite sprite = null;

            if (!string.IsNullOrEmpty(spriteGuid))
            {
                var spriteData = atlasTexture.GetSpriteDataFromGuid(spriteGuid);

                if (spriteData != null)
                {
                    sprite = atlasTexture.GetSpriteFromGuid(spriteGuid);
                }
            }

            Sprite = sprite;

            targetImage.sprite = Sprite;
        }

        private void Empty()
        {
            Sprite = null;

            if (targetImage != null)
            {
                targetImage.sprite = null;
            }
        }
    }
}
