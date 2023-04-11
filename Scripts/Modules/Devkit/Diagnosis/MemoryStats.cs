
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
		private TextMeshProUGUI totalAllocatedText = null;
		[SerializeField]
		private TextMeshProUGUI totalReservedText = null;
		[SerializeField]
		private TextMeshProUGUI unusedReservedText = null;
		[SerializeField]
		private TextMeshProUGUI monoHeapSizeText = null;
		[SerializeField]
		private TextMeshProUGUI monoUsedSizeText = null;
		[SerializeField]
		private TextMeshProUGUI allocatedMemoryForGraphicsDriverText = null;

		private float oldTime = 0f;

		private bool initialized = false;

		//----- property -----

		//----- method -----

		void Awake()
		{
			Initialize();
		}

		private bool IsEnable()
		{
			return UnityEngine.Debug.isDebugBuild;
		}

		public void Initialize()
		{
			if (initialized) { return; }

			if (!IsEnable()) { return; }
			
			oldTime = Time.realtimeSinceStartup;

			SetMemoryStats();

			initialized = true;
		}

		void Update()
		{
			if (!initialized) { return; }

			if (!IsEnable()) { return; }

			var time = Time.realtimeSinceStartup - oldTime;

			if (time >= UpdateInterval)
			{
				oldTime = Time.realtimeSinceStartup;

				SetMemoryStats();
			}
		}

		private void SetMemoryStats()
		{
			void SetMemoryStatsText(TextMeshProUGUI target, string label, long byteSize)
			{
				target.text = $"{label} : { ByteDataUtility.GetBytesReadable(byteSize) }";
			}

			SetMemoryStatsText(totalAllocatedText, "TotalAllocated", Profiler.GetTotalUnusedReservedMemoryLong());
			SetMemoryStatsText(totalReservedText, "TotalReserved", Profiler.GetTotalReservedMemoryLong());
			SetMemoryStatsText(unusedReservedText, "TotalUnusedReserved", Profiler.GetTotalUnusedReservedMemoryLong());
			SetMemoryStatsText(monoHeapSizeText, "MonoHeapSize", Profiler.GetMonoHeapSizeLong());
			SetMemoryStatsText(monoUsedSizeText, "MonoUsedSize", Profiler.GetMonoUsedSizeLong());
			SetMemoryStatsText(allocatedMemoryForGraphicsDriverText, "AllocatedForGraphicsDriver", Profiler.GetAllocatedMemoryForGraphicsDriver());
		}
	}
}