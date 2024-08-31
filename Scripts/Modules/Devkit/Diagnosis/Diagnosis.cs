
using Extensions;
using UnityEngine;
using Modules.Devkit.Diagnosis.SRDebugger;

namespace Modules.Devkit.Diagnosis
{
	public sealed class Diagnosis : SingletonMonoBehaviour<Diagnosis>
	{
		//----- params -----

		public sealed class Prefs
		{
			public static bool displayFpsStats
			{
				get { return SecurePrefs.GetBool(typeof(Prefs).FullName + "-displayFpsStats", true); }
				set { SecurePrefs.SetBool(typeof(Prefs).FullName + "-displayFpsStats", value); }
			}

			public static bool displayMemoryStats
			{
				get { return SecurePrefs.GetBool(typeof(Prefs).FullName + "-displayMemoryStats", false); }
				set { SecurePrefs.SetBool(typeof(Prefs).FullName + "-displayMemoryStats", value); }
			}
		}

		//----- field -----

		[SerializeField]
		private GameObject touchBlock = null;
		[SerializeField]
		private GameObject rootObject = null;
		[SerializeField]
		private FpsStats fpsStats = null;
		[SerializeField]
		private MemoryStats memoryStats = null;
		[SerializeField]
		private SRDiagnosis srDiagnosis = null;
		[SerializeField]
		private GameObject[] hideFpsContents = null;

		//----- property -----

		public GameObject RootObject { get { return rootObject; } }

		public FpsStats FpsStats { get { return fpsStats; } }

		public MemoryStats MemoryStats { get { return memoryStats; } }

		public SRDiagnosis SRDiagnosis { get { return srDiagnosis; } }

		public bool DisplayFpsStats
		{
			get { return Prefs.displayFpsStats; }

			set
			{
				Prefs.displayFpsStats = value;

				foreach (var item in hideFpsContents)
				{
					UnityUtility.SetActive(item, value);
				}
			}
		}

		public bool DisplayMemoryStats
		{
			get { return Prefs.displayMemoryStats; }

			set
			{
				Prefs.displayMemoryStats = value;
				UnityUtility.SetActive(memoryStats, value);
			}
		}

		//----- method -----

		public void Initialize(bool srDiagnosisEnable = true)
		{
			SetTouchBlock(touchBlock);

			UnityUtility.SetActive(gameObject, srDiagnosisEnable);

            #if ENABLE_DEVKIT

            fpsStats.IsEnable = true;
            memoryStats.IsEnable = true;
            srDiagnosis.IsEnable = true;

            #endif

            // FPS.

            fpsStats.Initialize();

			DisplayFpsStats = Prefs.displayFpsStats;

			// Memory.

			memoryStats.Initialize();

			DisplayMemoryStats = Prefs.displayMemoryStats;

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
