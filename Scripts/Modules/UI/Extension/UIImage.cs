﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Extensions;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Experimental.U2D;

#endif

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Image))]
    public abstract class UIImage : UIComponent<Image>
    {
        //----- params -----

        //----- field -----

        #pragma warning disable 0414

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

        //----- method -----

        void OnEnable()
        {
            #if UNITY_EDITOR

            ApplyDevelopmentAsset();

            #endif
        }

        void OnDisable()
        {
            #if UNITY_EDITOR

            DeleteCreatedAsset();

            #endif
        }

        #if UNITY_EDITOR

        private void ApplyDevelopmentAsset()
        {
            if (Application.isPlaying) { return; }

            Sprite sprite = null;

            if (!string.IsNullOrEmpty(assetGuid) && !string.IsNullOrEmpty(spriteId))
            {
                DeleteCreatedAsset();

                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (string.IsNullOrEmpty(assetPath)) { return; }

                var spriteAsset = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .OfType<Sprite>()
                    .FirstOrDefault(x => x.GetSpriteID().ToString() == spriteId);

                if (spriteAsset == null) { return; }

                var texture = spriteAsset.texture;
                var rect = spriteAsset.rect;
                var pivot = spriteAsset.pivot;
                var pixelsPerUnit = spriteAsset.pixelsPerUnit;
                var border = spriteAsset.border;

                sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);

                sprite.hideFlags = HideFlags.DontSaveInEditor;
            }

            Image.sprite = sprite;
        }

        private void DeleteCreatedAsset()
        {
            if (Application.isPlaying) { return; }

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            if (string.IsNullOrEmpty(spriteId)) { return; }

            if (Image != null)
            {
                var sprite = Image.sprite;

                if (sprite != null && sprite.hideFlags.HasFlag(HideFlags.DontSaveInEditor))
                {
                    Image.sprite = null;

                    UnityUtility.SafeDelete(sprite);
                }
            }
        }

        #endif
    }
}
