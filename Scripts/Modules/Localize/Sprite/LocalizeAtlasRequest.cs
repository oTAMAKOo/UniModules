
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Scene;

namespace Modules.Localize
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SceneBase))]
	public sealed class LocalizeAtlasRequest : MonoBehaviour
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
			if (folderGuids.IsEmpty()){ return; }

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
			if (folderGuids.IsEmpty()){ return; }

			var localizeAtlasManager = LocalizeAtlasManager.Instance;

			foreach (var folderGuid in folderGuids)
			{
				if (string.IsNullOrEmpty(folderGuid)){ continue; }

				var folderPath = localizeAtlasManager.GetFolderPathFromGuid(folderGuid);

				if (string.IsNullOrEmpty(folderPath)){ continue; }

				localizeAtlasManager.ReleaseAtlas(folderPath);
			}
		}

		private static LocalizeAtlasRequest FindInstance(UnityEngine.SceneManagement.Scene scene)
		{
			var rootObjects = scene.GetRootGameObjects();

			foreach (var rootObject in rootObjects)
			{
				var sceneBase = UnityUtility.GetComponent<SceneBase>(rootObject);

				if (sceneBase == null){ continue; }

				var atlasRequest = UnityUtility.GetComponent<LocalizeAtlasRequest>(sceneBase);

				if (atlasRequest != null)
				{
					return atlasRequest;
				}
			}

			return null;
		}

		public static async UniTask ForceRequest()
		{
			var sceneCount = SceneManager.sceneCount;

			var tasks = new List<UniTask>();

			for (var i = 0; i < sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);

				var atlasRequest = FindInstance(scene);

				if (atlasRequest != null)
				{
					var task = UniTask.Defer(() => atlasRequest.RequestAtlas());

					tasks.Add(task);
				}
			}

			await UniTask.WhenAll(tasks);
		}
	}
}