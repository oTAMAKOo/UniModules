
using UnityEngine;
using UnityEngine.Profiling;
using Extensions;
using TMPro;

namespace Modules.Devkit.Diagnosis
{
	public sealed class MemoryStats : MonoBehaviour
	{
		//----- params -----

		public const float UpdateInterval = 0.5f;

		//----- field -----

		[SerializeField]
		private TextMeshProUGUI monoMemoryText = null;
		[SerializeField]
		private TextMeshProUGUI heapMemoryText = null;
		[SerializeField]
		private TextMeshProUGUI graphicsMemoryText = null;

		private float oldTime = 0f;

        private bool? isEnable = null;

        private bool initialized = false;

        //----- property -----

        public bool IsEnable
        {
            get { return isEnable.HasValue ? isEnable.Value : UnityEngine.Debug.isDebugBuild; }
            set { isEnable = value; }
        }

		//----- method -----

		void Awake()
		{
			Initialize();
		}

		public void Initialize()
		{
			if (initialized) { return; }

			if (!IsEnable) { return; }

			oldTime = Time.realtimeSinceStartup;

			SetMemoryStats();

			initialized = true;
		}

		void Update()
		{
			if (!initialized) { return; }

			if (!IsEnable) { return; }

			var time = Time.realtimeSinceStartup - oldTime;

			if (time >= UpdateInterval)
			{
				oldTime = Time.realtimeSinceStartup;

				SetMemoryStats();
			}
		}

		private void SetMemoryStats()
		{
			// Mono Heap : Scriptからnewしたものなど.

			var monoUsedSize = Profiler.GetMonoUsedSizeLong();
			var monoHeapSize = Profiler.GetMonoHeapSizeLong();

			monoMemoryText.text = $"Mono Heap : { ByteDataUtility.GetBytesReadable(monoUsedSize) } / { ByteDataUtility.GetBytesReadable(monoHeapSize) }";

			// Unity Memory : リソースなど.

			var totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
			var totalReserved = Profiler.GetTotalReservedMemoryLong();

			heapMemoryText.text = $"Unity Memory : { ByteDataUtility.GetBytesReadable(totalAllocated) } / { ByteDataUtility.GetBytesReadable(totalReserved) }";

			// Graphic Driver : グラフィックス.
			// ※ Editor / Development Buildのみ動作.

			if (Application.isEditor || Debug.isDebugBuild)
			{
				var graphicsMemory = Profiler.GetAllocatedMemoryForGraphicsDriver();

				graphicsMemoryText.text = $"Graphics : { ByteDataUtility.GetBytesReadable(graphicsMemory) }";

				UnityUtility.SetActive(graphicsMemoryText, true);
			}
			else
			{
				UnityUtility.SetActive(graphicsMemoryText, false);
			}
		}
	}
}