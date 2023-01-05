
using UnityEditor;
using Unity.Linq;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif 

namespace Modules.Devkit.CleanComponent
{
	#if UNITY_2018_3_OR_NEWER

	public sealed class PrefabModeDummyTextCleaner
	{
		//----- params -----

		//----- field -----

		//----- property -----

		//----- method -----

		[InitializeOnLoadMethod]
		private static void InitializeOnLoadMethod()
		{
			PrefabModeEventHook.OnClosePrefabModeAsObservable().Subscribe(x => ClosePrefabMode(x));
		}

		private static void ClosePrefabMode(PrefabStage prefabStage)
		{
			var changed = false;

			var gameObjects = prefabStage.prefabContentsRoot.DescendantsAndSelf();

			foreach (var gameObject in gameObjects)
			{
				changed |= DummyTextCleaner.ModifyComponents(gameObject);
			}

			if (changed)
			{
				var prefabRoot = prefabStage.prefabContentsRoot;
				var assetPath = prefabStage.assetPath;

				PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
			}
		}
	}

	#endif
}
