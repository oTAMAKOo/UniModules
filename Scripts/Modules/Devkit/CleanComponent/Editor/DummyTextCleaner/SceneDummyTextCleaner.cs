
using UnityEditor;
using UnityEditor.SceneManagement;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;

namespace Modules.Devkit.CleanComponent
{
    public sealed class SceneDummyTextCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		[InitializeOnLoadMethod]
		private static void InitializeOnLoadMethod()
		{
			CurrentSceneSaveHook.OnSaveSceneAsObservable()
				.Subscribe(x =>
					{
						Clean(x);
						ReApply(x).Forget();
					});
		}

		public static void Clean()
		{
			var activeScene = EditorSceneManager.GetActiveScene();

			Clean(activeScene.path);
		}

		private static void Clean(string sceneAssetPath)
		{
			var activeScene = EditorSceneManager.GetActiveScene();

			if (activeScene.path != sceneAssetPath) { return; }

			var rootGameObjects = activeScene.GetRootGameObjects();

			foreach (var rootGameObject in rootGameObjects)
			{
				DummyTextCleaner.ModifyComponents(rootGameObject);
			}
		}

		private static async UniTask ReApply(string sceneAssetPath)
		{
			await UniTask.NextFrame();

			var activeScene = EditorSceneManager.GetActiveScene();

			if (activeScene.path != sceneAssetPath) { return; }

			var rootGameObjects = activeScene.GetRootGameObjects();

			foreach (var rootGameObject in rootGameObjects)
			{
				DummyTextCleaner.ReApply(rootGameObject);
			}
		}
    }
}