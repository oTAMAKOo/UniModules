﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Atlas
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Image))]
    public class AtlasTextureImage : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private AtlasTexture atlas = null;
        [SerializeField]
        private string spriteName = null;
        [SerializeField]
        private string spriteGuid = null;

        private Image targetImage = null;

        //----- property -----

        public AtlasTexture Atlas
        {
            get { return atlas; }
            set { atlas = value; }
        }

        public string SpriteName
        {
            get { return spriteName; }

            set
            {
                SpriteData spriteData = null;

                if (atlas != null)
                {
                    spriteData = atlas.GetSpriteData(value);

                    if (spriteData == null)
                    {
                        if (!string.IsNullOrEmpty(spriteGuid))
                        {
                            spriteData = atlas.GetSpriteData(spriteGuid);
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

            if (targetImage != null && atlas != null)
            {
                var spriteData = atlas.GetSpriteData(spriteGuid);

                if (spriteData != null)
                {
                    spriteName = spriteData.name;
                }

                Sprite = string.IsNullOrEmpty(spriteName) ? null : atlas.GetSprite(spriteName);

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
