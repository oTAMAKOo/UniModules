﻿
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.SpriteSheet
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Image))]
    public class SpriteSheetImage : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private SpriteSheet spriteSheet = null;
        [SerializeField]
        private string spriteName = null;
        [SerializeField]
        private string spriteGuid = null;

        private Image targetImage = null;

        //----- property -----

        public SpriteSheet SpriteSheet
        {
            get { return spriteSheet; }
            set { spriteSheet = value; }
        }

        public string SpriteName
        {
            get { return spriteName; }

            set
            {
                SpriteData spriteData = null;

                if (spriteSheet != null)
                {
                    spriteData = spriteSheet.GetSpriteData(value);

                    if (spriteData == null)
                    {
                        if (!string.IsNullOrEmpty(spriteGuid))
                        {
                            spriteData = spriteSheet.GetSpriteData(spriteGuid);
                        }
                    }
                }

                if (spriteData != null)
                {
                    spriteGuid = spriteData.guid;
                    spriteName = value;

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

            if (targetImage != null && spriteSheet != null)
            {
                var spriteData = spriteSheet.GetSpriteData(spriteGuid);

                if (spriteData != null)
                {
                    spriteName = spriteData.name;
                }

                Sprite = string.IsNullOrEmpty(spriteName) ? null : spriteSheet.GetSprite(spriteName);

                targetImage.sprite = Sprite;
            }
        }

        private void Empty()
        {
            spriteGuid = null;
            spriteName = null;
            Sprite = null;

            if (targetImage != null)
            {
                targetImage.sprite = null;
            }
        }
    }
}
