
using Extensions;
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

		public FpsStats FpsStats { get { return fpsStats; } }

		public SRDiagnosis SRDiagnosis { get { return srDiagnosis; } }

		//----- method -----

		public void Initialize(bool displayFps = true, bool srDiagnosisEnable = true)
		{
			SetTouchBlock(touchBlock);

			UnityUtility.SetActive(gameObject, displayFps || srDiagnosisEnable);

			// FPS.

			if (displayFps)
			{
				fpsStats.Initialize();
			}

			UnityUtility.SetActive(fpsStats, displayFps);

			// SRDebugger.

			#if ENABLE_SRDEBUGGER

			if (srDiagnosisEnable)
			{
				srDiagnosis.Initialize();
			}

			#endif

			UnityUtility.SetActive(srDiagnosis, srDiagnosisEnable);
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
