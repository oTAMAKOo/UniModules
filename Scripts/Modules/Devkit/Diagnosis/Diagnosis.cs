
using UnityEngine;
using Modules.Devkit.Diagnosis.SRDebugger;

namespace Modules.Devkit.Diagnosis
{
    public sealed class Diagnosis : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameObject touchBlock = null;
        [SerializeField]
        private FpsStats fpsStats = null;
        [SerializeField]
        private SRDiagnosis srDiagnosis = null;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            fpsStats.Initialize();

            #if ENABLE_SRDEBUGGER

            srDiagnosis.Initialize();

            #endif

            SetTouchBlock(touchBlock);
        }

        public void SetTouchBlock(GameObject touchBlock)
        {
            this.touchBlock = touchBlock;

            #if ENABLE_SRDEBUGGER

            srDiagnosis.SetTouchBlock(touchBlock);

            #endif
        }
    }
}
