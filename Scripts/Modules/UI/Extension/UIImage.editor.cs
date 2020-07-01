
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

        private sealed class AssetCacheInfo
        {
            public string assetGuid { get; set; }
            public string spriteId { get; set; }
            public Sprite spriteAsset { get; set; }
        }

        //----- field -----

        #pragma warning disable 0414

        [SerializeField, HideInInspector]
        private string assetGuid = null;
        [SerializeField, HideInInspector]
        private string spriteId = null;

        private static FixedQueue<AssetCacheInfo> spriteAssetCache = null;

        #pragma warning restore 0414

        //----- property -----

        //----- method -----

        private void ApplyDevelopmentAsset()
        {
            if (Application.isPlaying){ return; }

            if (Image.sprite != null) { return; }

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            if (string.IsNullOrEmpty(spriteId)) { return; }

            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            if (string.IsNullOrEmpty(assetPath)) { return; }
            
            if (spriteAssetCache == null)
            {
                spriteAssetCache = new FixedQueue<AssetCacheInfo>(250);
            }

            Sprite spriteAsset = null;

            var cacheAssetInfo = spriteAssetCache.FirstOrDefault(x => x.assetGuid == assetGuid && x.spriteId == spriteId);

            if (cacheAssetInfo == null)
            {
                spriteAsset = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                    .OfType<Sprite>()
                    .FirstOrDefault(x => x.GetSpriteID().ToString() == spriteId);

                if (spriteAsset != null)
                {
                    cacheAssetInfo = new AssetCacheInfo()
                    {
                        assetGuid = assetGuid,
                        spriteId = spriteId,
                        spriteAsset = spriteAsset,
                    };

                    spriteAssetCache.Enqueue(cacheAssetInfo);
                }
            }
            else
            {
                spriteAsset = cacheAssetInfo.spriteAsset;

                spriteAssetCache.Remove(cacheAssetInfo);

                spriteAssetCache.Enqueue(cacheAssetInfo);
            }

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
