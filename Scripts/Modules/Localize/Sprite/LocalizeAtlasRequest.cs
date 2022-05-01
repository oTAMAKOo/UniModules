
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.SceneManagement;

namespace Modules.Localize
{
	public sealed class LocalizeAtlasRequest : MonoBehaviour, ISceneEvent
    {
        //----- params -----

        //----- field -----

		[SerializeField]
		private string[] folderGuids = null;

		//----- property -----

		public IReadOnlyList<string> FolderGuids
		{
			get { return folderGuids ?? (folderGuids = new string[0]); }
		}

		//----- method -----

		public async UniTask OnLoadScene()
		{
			await RequestAtlas();
		}

		public UniTask OnUnloadScene()
		{
			ReleaseAtlas();

			return UniTask.CompletedTask;
		}

		public async UniTask RequestAtlas()
		{
			var localizeAtlasManager = LocalizeAtlasManager.Instance;

			var tasks = new List<UniTask>();

			foreach (var folderGuid in folderGuids)
			{
				if (string.IsNullOrEmpty(folderGuid)){ continue; }

				var folderPath = localizeAtlasManager.GetFolderPathFromGuid(folderGuid);

				if (string.IsNullOrEmpty(folderPath)){ continue; }

				var task = UniTask.Defer(() => localizeAtlasManager.LoadAtlas(folderPath));

				tasks.Add(task);
			}

			await UniTask.WhenAll(tasks);
		}

		public void ReleaseAtlas()
		{
			var localizeAtlasManager = LocalizeAtlasManager.Instance;

			foreach (var folderGuid in folderGuids)
			{
				if (string.IsNullOrEmpty(folderGuid)){ continue; }

				var folderPath = localizeAtlasManager.GetFolderPathFromGuid(folderGuid);

				if (string.IsNullOrEmpty(folderPath)){ continue; }

				localizeAtlasManager.ReleaseAtlas(folderPath);
			}
		}

		public static async UniTask ForceRequest()
		{
			var sceneCount = SceneManager.sceneCount;

			var tasks = new List<UniTask>();

			for (var i = 0; i < sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);

				var rootObjects = scene.GetRootGameObjects();

				foreach (var rootObject in rootObjects)
				{
					var targets = UnityUtility.FindObjectsOfInterface<LocalizeAtlasRequest>(rootObject);

					foreach (var target in targets)
					{
						var task = UniTask.Defer(() => target.RequestAtlas());

						tasks.Add(task);
					}
				}
			}

			await UniTask.WhenAll(tasks);
		}
	}
}