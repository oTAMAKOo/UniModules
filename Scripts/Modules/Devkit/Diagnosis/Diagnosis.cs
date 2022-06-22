
using UnityEngine;
using Extensions;
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

		public void Initialize(bool fpsEnable = true, bool srDiagnosisEnable = true)
		{
			// FPS.

			if (fpsEnable)
			{
				fpsStats.Initialize();
			}

			UnityUtility.SetActive(fpsStats, fpsEnable);

			// SRDebugger.

			#if ENABLE_SRDEBUGGER

			if (srDiagnosisEnable)
			{
				srDiagnosis.Initialize();
			}

			#endif

			UnityUtility.SetActive(srDiagnosis, srDiagnosisEnable);

			// Other.

			SetTouchBlock(touchBlock);

			UnityUtility.SetActive(gameObject, fpsEnable || srDiagnosisEnable);
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
