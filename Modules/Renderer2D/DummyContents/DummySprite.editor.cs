
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;

#if UNITY_2019_4_OR_NEWER
using UnityEditor.U2D;
#else
using UnityEditor.Experimental.U2D;
#endif

namespace Modules.Renderer2D.DummyContent
{
    public sealed partial class DummySprite
    {
        //----- params -----

        public const string DummyAssetName = "*Sprite (DummyAsset)";

        private sealed class AssetCacheInfo
        {
            public string assetGuid { get; set; }
            public string spriteId { get; set; }
            public Sprite spriteAsset { get; set; }
        }

        //----- field -----

        private static FixedQueue<AssetCacheInfo> spriteAssetCache = null;

        //----- property -----

        //----- method -----

        private void ApplyDummyAsset()
        {
            if (Application.isPlaying) { return; }

            if (BuildPipeline.isBuildingPlayer) { return; }

            if (SpriteRenderer.sprite != null && SpriteRenderer.sprite.name != DummyAssetName) { return; }

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

            DeleteCreatedAsset();

            var texture = spriteAsset.texture;
            var rect = spriteAsset.textureRect;
            var rawPivot = spriteAsset.pivot;
            var pixelsPerUnit = spriteAsset.pixelsPerUnit;
            var border = spriteAsset.border;

            // Sprite.Create は pivot を 0〜1 の正規化値で要求する一方、Sprite.pivot はピクセル絶対値を返すため正規化する.
            var pivot = new Vector2(
                rect.width > 0 ? rawPivot.x / rect.width : 0.5f,
                rect.height > 0 ? rawPivot.y / rect.height : 0.5f);

            var sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);

            sprite.name = DummyAssetName;

            sprite.hideFlags = HideFlags.DontSaveInEditor;

            SpriteRenderer.sprite = sprite;
        }

        private void DeleteCreatedAsset()
        {
            if (Application.isPlaying) { return; }

            if (SpriteRenderer == null) { return; }

            var sprite = SpriteRenderer.sprite;

            if (sprite != null && sprite.name == DummyAssetName)
            {
                SpriteRenderer.sprite = null;

                UnityUtility.SafeDelete(sprite, true);
            }
        }
    }
}

#endif
