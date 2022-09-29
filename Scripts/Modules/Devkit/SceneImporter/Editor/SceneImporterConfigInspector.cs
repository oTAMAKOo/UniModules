
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UniRx;
using Extensions.Devkit;
using Extensions;
using Modules.Devkit.Inspector;
using Object = UnityEngine.Object;

namespace Modules.Devkit.SceneImporter
{
    [CustomEditor(typeof(SceneImporterConfig))]
    public sealed class SceneImporterConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

		private FolderRegisterScrollView sceneFolderView = null;

		private LifetimeDisposable lifetimeDisposable = null;

        private SceneImporterConfig instance = null;

        //----- property -----

        //----- method -----

		void OnEnable()
		{
			instance = target as SceneImporterConfig;

			lifetimeDisposable = new LifetimeDisposable();

			sceneFolderView = new FolderRegisterScrollView("Scene Folders", nameof(SceneImporterConfigInspector) + "-ManagedFolders");

			sceneFolderView.RemoveChildrenFolder = true;

			sceneFolderView.OnUpdateContentsAsObservable()
				.Subscribe(x => SaveCompressFolders(x.Select(y => y.asset).ToArray()))
				.AddTo(lifetimeDisposable.Disposable);

			var managedFolders = Reflection.GetPrivateField<SceneImporterConfig, Object[]>(instance, "managedFolders");

			var managedFolderGuids = managedFolders
				.Select(x => UnityEditorUtility.GetAssetGUID(x))
				.ToArray();

			sceneFolderView.SetContents(managedFolderGuids);
		}

        public override void OnInspectorGUI()
        {
            instance = target as SceneImporterConfig;
			
            var contentHeight = 16f;

			EditorGUILayout.Separator();

            // InitialScene.

			var initialScene = Reflection.GetPrivateField<SceneImporterConfig, Object>(instance, "initialScene");

			EditorGUI.BeginChangeCheck();

			initialScene = EditorGUILayout.ObjectField("Initial Scene", initialScene, typeof(SceneAsset), false, GUILayout.Height(contentHeight));

			if (EditorGUI.EndChangeCheck())
			{
				UnityEditorUtility.RegisterUndo(instance);

				Reflection.SetPrivateField<SceneImporterConfig, Object>(instance, "initialScene", initialScene);
			}

            EditorGUILayout.Separator();

            // AutoAdditionFolders.

			sceneFolderView.DrawGUI();
		}

		private void SaveCompressFolders(Object[] folders)
		{
			UnityEditorUtility.RegisterUndo(instance);

			Reflection.SetPrivateField(instance, "managedFolders", folders);
		}
    }
}
