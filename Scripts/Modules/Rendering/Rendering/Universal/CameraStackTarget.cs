
#if ENABLE_UNIVERSALRENDERPIPELINE
using Extensions;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Modules.Rendering.Universal
{
    [RequireComponent(typeof(UniversalAdditionalCameraData))]
    public sealed class CameraStackTarget : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private uint priority = 0;
        [SerializeField]
        private bool autoStack = false;

        private Camera targetCamera = null;
        private UniversalAdditionalCameraData cameraData = null;

        //----- property -----

        public uint Priority { get { return priority; } }

        public Camera Camera
        {
            get
            {
                return targetCamera ?? (targetCamera = UnityUtility.GetComponent<Camera>(gameObject));
            }
        }

        public UniversalAdditionalCameraData CameraData
        {
            get
            {
                return cameraData ?? (cameraData = UnityUtility.GetComponent<UniversalAdditionalCameraData>(gameObject));
            }
        }

        //----- method -----

        void OnEnable()
        {
            var cameraStackManager = CameraStackManager.Instance;

            if (autoStack)
            {
                cameraStackManager.AddStackCamera(Camera);
                cameraStackManager.UpdateCurrentCameraStack();
            }
        }
    }
}

#endif
