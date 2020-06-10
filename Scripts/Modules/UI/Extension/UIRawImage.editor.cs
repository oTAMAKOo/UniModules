
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using Extensions;

namespace Modules.UI.Extension
{
    public abstract partial class UIRawImage
    {
        //----- params -----

        public const string DevelopmentAssetName = "*Texture (Development)";

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
            if (Application.isPlaying) { return; }

            Texture2D texture = null;

            if (!string.IsNullOrEmpty(assetGuid))
            {
                DeleteCreatedAsset();

                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (string.IsNullOrEmpty(assetPath)) { return; }

                var textureAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture2D;

                if (textureAsset == null) { return; }

                texture = new Texture2D(textureAsset.width, textureAsset.height, TextureFormat.ARGB32, false);

                texture.name = DevelopmentAssetName;

                texture.hideFlags = HideFlags.DontSaveInEditor;

                Graphics.ConvertTexture(textureAsset, texture);

                // Bug: UnityのバグでこのタイミングでアクティブなRenderTextureを空にしないと下記警告が出る.
                // 「Releasing render texture that is set to be RenderTexture.active!」.
                RenderTexture.active = null;
            }

            RawImage.texture = texture;
        }

        private void DeleteCreatedAsset()
        {
            if (Application.isPlaying) { return; }

            if (RawImage != null)
            {
                var texture = RawImage.texture;

                if (texture != null && texture.name == DevelopmentAssetName)
                {
                    RawImage.texture = null;

                    UnityUtility.SafeDelete(texture);
                }
            }
        }
    }
}

#endif
