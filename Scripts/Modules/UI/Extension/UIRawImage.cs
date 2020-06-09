
using Extensions;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RawImage))]
    public abstract class UIRawImage : UIComponent<RawImage>
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

        public RawImage RawImage { get { return component; } }

        public Texture texture
        {
            get { return component.texture; }
            set { component.texture = value; }
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

            Texture2D texture = null;
            
            if (!string.IsNullOrEmpty(assetGuid))
            {
                DeleteCreatedAsset();

                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                if (string.IsNullOrEmpty(assetPath)) { return; }

                var textureAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture2D;

                if (textureAsset == null) { return; }
                
                texture = new Texture2D(textureAsset.width, textureAsset.height, TextureFormat.ARGB32, false);

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

            if (string.IsNullOrEmpty(assetGuid)) { return; }

            if (RawImage != null)
            {
                var texture = RawImage.texture;

                if (texture != null && texture.hideFlags.HasFlag(HideFlags.DontSaveInEditor))
                {
                    RawImage.texture = null;

                    UnityUtility.SafeDelete(texture);
                }
            }
        }

        #endif
    }
}
