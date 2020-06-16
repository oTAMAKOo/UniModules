
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using System.Linq;
using Extensions;

namespace Modules.UI.Extension
{
    public abstract partial class UIImage
    {
        //----- params -----

        public const string DevelopmentAssetName = "*Sprite (Development)";

        //----- field -----

        #pragma warning disable 0414

        [SerializeField, HideInInspector]
        private string assetGuid = null;
        [SerializeField, HideInInspector]
        private string spriteId = null;

        #pragma warning restore 0414

        //----- property -----

        //----- method -----

        private void ApplyDevelopmentAsset()
        {
            if (Application.isPlaying){ return; }

            DeleteCreatedAsset();

            if (Image.sprite != null) { return; }

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            if (string.IsNullOrEmpty(spriteId)) { return; }
        
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

            var sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);

            sprite.name = DevelopmentAssetName;

            sprite.hideFlags = HideFlags.DontSaveInEditor;

            Image.sprite = sprite;
        }

        private void DeleteCreatedAsset()
        {
            if (Application.isPlaying) { return; }

            if (Image == null){ return; }

            var sprite = Image.sprite;

            if (sprite != null && sprite.name == DevelopmentAssetName)
            {
                Image.sprite = null;

                UnityUtility.SafeDelete(sprite);
            }
        }
    }
}

#endif
