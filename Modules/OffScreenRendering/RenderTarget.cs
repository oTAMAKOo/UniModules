
using UnityEngine;
using Extensions;

namespace Modules.OffScreenRendering
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class RenderTarget : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private int width = 100;
        [SerializeField]
        private int height = 100;
        [SerializeField]
        private int depth = 16;
        [SerializeField]
        private RenderTextureFormat format = RenderTextureFormat.ARGB32;

        private Vector2 size = Vector2.zero;
        private Camera renderCamera = null;
        private RenderTexture renderTexture = null;

        //----- property -----

        public RenderTexture RenderTexture
        {
            get { return renderTexture; }
        }

        public Camera RenderCamera { get { return renderCamera; } }

        //----- method -----

        void Awake()
        {
            CreateRenderTexture(width, height, depth, format);
        }

        public RenderTexture CreateRenderTexture(int width, int height, int depth = 16, RenderTextureFormat format = RenderTextureFormat.ARGB32)
        {
            if (renderTexture != null && size.x == width && size.y == height)
            {
                return renderTexture;
            }

            if (renderCamera == null)
            {
                renderCamera = UnityUtility.GetComponent<Camera>(gameObject);
            }

            if (renderTexture != null)
            {
                UnityUtility.SafeDelete(renderTexture);
            }

            this.width = width;
            this.height = height;
            this.depth = depth;
            this.format = format;

            renderTexture = new RenderTexture(width, height, depth, format);

            if (renderTexture != null)
            {
                renderTexture.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }

            renderCamera.targetTexture = renderTexture;

            return renderTexture;
        }
    }
}
