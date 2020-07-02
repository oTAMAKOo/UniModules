
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Extensions;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Modules.Devkit.DummyAssetSetter
{
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public sealed class DummyTextureSetter : MonoBehaviour
    {
        #if UNITY_EDITOR

        //----- params -----

        public const string DummyAssetName = "*Texture (DummyAsset)";

        private sealed class AssetCacheInfo
        {
            public string assetGuid { get; set; }
            public Texture textureAsset { get; set; }
        }

        //----- field -----

        [SerializeField, HideInInspector]
        private string assetGuid = null;

        private RawImage rawImage = null;

        private static FixedQueue<AssetCacheInfo> textureAssetCache = null;

        //----- property -----

        public RawImage RawImage
        {
            get { return rawImage ?? (rawImage = UnityUtility.GetComponent<RawImage>(gameObject)); }
        }

        //----- method -----

        void OnEnable()
        {
            ApplyDummyAsset();
        }

        void OnDisable()
        {
            DeleteCreatedAsset();
        }

        private void ApplyDummyAsset()
        {
            if (Application.isPlaying) { return; }

            DeleteCreatedAsset();

            if (RawImage.texture != null && RawImage.texture.name != DummyAssetName) { return; }

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            if (string.IsNullOrEmpty(assetPath)) { return; }

            if (textureAssetCache == null)
            {
                textureAssetCache = new FixedQueue<AssetCacheInfo>(100);
            }

            Texture textureAsset = null;

            var cacheAssetInfo = textureAssetCache.FirstOrDefault(x => x.assetGuid == assetGuid);

            if (cacheAssetInfo == null)
            {
                textureAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture;

                if (textureAsset != null)
                {
                    cacheAssetInfo = new AssetCacheInfo()
                    {
                        assetGuid = assetGuid,
                        textureAsset = textureAsset,
                    };

                    textureAssetCache.Enqueue(cacheAssetInfo);
                }
            }
            else
            {
                textureAsset = cacheAssetInfo.textureAsset;

                textureAssetCache.Remove(cacheAssetInfo);

                textureAssetCache.Enqueue(cacheAssetInfo);
            }

            if (textureAsset == null) { return; }

            DeleteCreatedAsset();

            var texture = new Texture2D(textureAsset.width, textureAsset.height, TextureFormat.ARGB32, false);

            texture.name = DummyAssetName;

            texture.hideFlags = HideFlags.DontSaveInEditor;

            Graphics.ConvertTexture(textureAsset, texture);

            // Bug: UnityのバグでこのタイミングでアクティブなRenderTextureを空にしないと下記警告が出る.
            // 「Releasing render texture that is set to be RenderTexture.active!」.
            RenderTexture.active = null;

            RawImage.texture = texture;
        }

        private void DeleteCreatedAsset()
        {
            if (Application.isPlaying) { return; }

            if (RawImage == null) { return; }

            var texture = RawImage.texture;

            if (texture != null && texture.name == DummyAssetName)
            {
                RawImage.texture = null;

                UnityUtility.SafeDelete(texture);
            }
        }
        
        #endif
    }
}
