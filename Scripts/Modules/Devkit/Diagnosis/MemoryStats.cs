
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
			// GC(Mono) 最大使用量 / 現在使用量.

			var monoUsedSize = Profiler.GetMonoUsedSizeLong();
			var monoHeapSize = Profiler.GetMonoHeapSizeLong();

			monoMemoryText.text = $"Heap / Used : { ByteDataUtility.GetBytesReadable(monoUsedSize) } / { ByteDataUtility.GetBytesReadable(monoHeapSize) }";

			// ヒープメモリ / 予約済みだが未割り当てのヒープメモリ.

			var totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
			var totalReserved = Profiler.GetTotalReservedMemoryLong();

			heapMemoryText.text = $"Alloc / Reserved : { ByteDataUtility.GetBytesReadable(totalAllocated) } / { ByteDataUtility.GetBytesReadable(totalReserved) }";

			// グラフィックで使用されているメモリ　※ Editor / Development Build.

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