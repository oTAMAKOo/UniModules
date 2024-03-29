
using UnityEngine;
using UnityEditor;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.AssemblyCompilation;

using Object = UnityEngine.Object;

namespace Modules.ExternalAssets
{
    public sealed class ManageWindow : SingletonEditorWindow<ManageWindow>
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(500f, 450f);

        //----- field -----

        private AssetManagement assetManagement = null;

        private HeaderView headerView = null;
        private ManageAssetView manageAssetView = null;

        //----- property -----

        //----- method -----

        public static void Open(string externalAssetPath, string shareResourcesPath)
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("Error", "Can not open while compiling.", "Close");
                return;
            }

			Instance.minSize = WindowSize;
            Instance.titleContent = new GUIContent("ExternalAsset Manage Asset");

            Instance.Initialize(externalAssetPath, shareResourcesPath);

            Instance.ShowUtility();

			Instance.CheckInvalidManageInfo();
		}

        private void Initialize(string externalAssetPath, string shareResourcesPath)
        {
            // コンパイルが始まったら閉じる.
            CompileNotification.OnCompileStartAsObservable()
                .Subscribe(_ => Close())
                .AddTo(Disposable);

            // AssetManagement.

            assetManagement = AssetManagement.Instance;

            assetManagement.Initialize();

            // HeaderView.

            headerView = new HeaderView();

            headerView.Initialize(assetManagement);

            headerView.OnRequestRepaintAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(Disposable);

            headerView.OnChangeSelectGroupAsObservable()
                .Subscribe(_ =>
                    {
                        manageAssetView.SetGroup(headerView.Group);
                    })
                .AddTo(Disposable);

            // ManageAssetView.

            manageAssetView = new ManageAssetView();

            manageAssetView.Initialize(assetManagement, externalAssetPath, shareResourcesPath);

            manageAssetView.OnRequestRepaintAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(Disposable);

            manageAssetView.SetGroup(headerView.Group);
        }

        void OnDestroy()
        {
            if (AssetManagement.Prefs.manifestUpdateRequest)
            {
                AssetInfoManifestGenerator.Generate().Forget();
            }

            Disposable.Dispose();
        }

        void OnGUI()
        {
            headerView.DrawGUI();
            
            if (headerView.IsGroupNameEdit)
            {
                EditorGUILayout.HelpBox("Can not operate while entering name.", MessageType.Warning);

                return;
            }
            else
            {
                if (string.IsNullOrEmpty(headerView.Group))
                {
                    EditorGUILayout.HelpBox("require select group.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.Separator();

                    manageAssetView.DrawGUI();

                    EditorGUILayout.Separator();
                }
            }

            UpdateDragAndDrop();
        }

		private void CheckInvalidManageInfo()
		{
			var managedAssets = ManagedAssets.Instance;

			var hasInvalid = managedAssets.GetAllInfos()
				.Any(x => string.IsNullOrEmpty(x.guid) || AssetDatabase.GUIDToAssetPath((string)x.guid) == string.Empty);

			if (hasInvalid)
			{
				var title = "ExternalAsset ManagedAssets";
				var message = "Contain invalid manage info.";

				EditorUtility.DisplayDialog(title, message, "close");

				Selection.activeObject = managedAssets;
			}
		}

        private void UpdateDragAndDrop()
        {
            var e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    var validate = false;

                    if (!string.IsNullOrEmpty(headerView.Group))
                    {
                        if (headerView.Group == ExternalAsset.ShareGroupName)
                        {
                            validate = assetManagement.IsShareResourcesTarget(DragAndDrop.objectReferences);
                        }
                        else
                        {
                            validate = assetManagement.IsExternalAssetTarget(DragAndDrop.objectReferences);
                        }
                    }

                    DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;

                        if (validate)
                        {
                            OnDragAndDrop(DragAndDrop.objectReferences).Forget();
                        }
                    }

                    break;
            }
        }

        public async UniTask OnDragAndDrop(Object[] assetObjects)
        {
            if (string.IsNullOrEmpty(headerView.Group)) { return; }

            var assetObject = assetObjects.FirstOrDefault();

            if (assetObject == null) { return; }

            if (!assetManagement.ValidateManageInfo(assetObject)) { return; }
            
            // 管理情報を追加.
            assetManagement.AddManageInfo(headerView.Group, assetObject);

            await manageAssetView.BuildManageInfoViews();

            Repaint();
        }
    }
}
