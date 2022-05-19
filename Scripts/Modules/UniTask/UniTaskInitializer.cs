using UnityEngine;
using UnityEngine.LowLevel;
using Cysharp.Threading.Tasks;

namespace Modules.UniTaskExtension
{
	public sealed class UniTaskInitializer
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		public static void OnAfterAssembliesLoaded()
		{
			// https://github.com/Cysharp/UniTask

			// The order in which methods are called in BeforeSceneLoad is nondeterministic,
			// so if you want to use UniTask in other BeforeSceneLoad methods,
			// you should try to initialize it before this.

			#if UNITY_2019_3_OR_NEWER

			var playerLoop =PlayerLoop.GetCurrentPlayerLoop();

			#else

            var playerLoop = PlayerLoop.GetDefaultPlayerLoop();

			#endif

			PlayerLoopHelper.Initialize(ref playerLoop);
		}
	}
}
