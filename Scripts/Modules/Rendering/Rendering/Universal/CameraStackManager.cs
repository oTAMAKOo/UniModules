
#if ENABLE_UNIVERSALRENDERPIPELINE

using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.Rendering.Universal
{
    public sealed class CameraStackManager : Singleton<CameraStackManager>
    {
        //----- params -----

        //----- field -----

        private Camera currentCamera = null;
        
        private List<CameraStackTarget> cameraStackTargets = null;

        //----- property -----

        //----- method -----

        protected override void OnCreate()
        {
            cameraStackTargets = new List<CameraStackTarget>();
        }

        public void SwitchMainCamera(Camera camera)
        {
            if (camera == null){ return; }

            var prevCamera = currentCamera;

            currentCamera = camera;

            //------ Remove Prev Camera ------

            var prevCameraData = UnityUtility.GetComponent<UniversalAdditionalCameraData>(prevCamera);

            if (prevCameraData != null)
            {
                prevCameraData.cameraStack.Clear();
            }
            
            //------ Setup New Camera ------

            UpdateCurrentCameraStack();
        }

        public void UpdateCurrentCameraStack()
        {
            if (currentCamera == null){ return; }

            var currentCameraData = UnityUtility.GetComponent<UniversalAdditionalCameraData>(currentCamera);

            if (currentCameraData != null)
            {
                currentCameraData.cameraStack.Clear();

                cameraStackTargets = cameraStackTargets
                    .Where(x => !UnityUtility.IsNull(x))
                    .OrderBy(x => x.Priority)
                    .ToList();

                foreach (var element in cameraStackTargets)
                {
                    currentCameraData.cameraStack.Add(element.Camera);
                }
            }
        }

        public void AddStackCamera(Camera camera)
        {
            if (camera == null){ return; }

            var cameraStackTarget = UnityUtility.GetComponent<CameraStackTarget>(camera);

            if (cameraStackTarget == null){ return; }

            if (!cameraStackTargets.Contains(cameraStackTarget))
            {
                cameraStackTargets.Add(cameraStackTarget);
            }
        }

        public void RemoveStackCamera(Camera camera)
        {
            if (camera == null){ return; }

            var cameraStackTarget = UnityUtility.GetComponent<CameraStackTarget>(camera);

            if (cameraStackTarget == null){ return; }

            if (cameraStackTargets.Contains(cameraStackTarget))
            {
                cameraStackTargets.Remove(cameraStackTarget);
            }
        }
    }
}

#endif
