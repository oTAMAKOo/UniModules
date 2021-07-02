
using UnityEngine;
using UnityEditor;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.AssemblyCompilation;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
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

        public static void Open(string externalResourcesPath, string shareResourcesPath)
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("Error", "Can not open while compiling.", "Close");
                return;
            }
            
            Instance.minSize = WindowSize;
            Instance.titleContent = new GUIContent("ExternalResource Manage Asset");

            Instance.Initialize(externalResourcesPath, shareResourcesPath);

            Instance.ShowUtility();
        }

        private void Initialize(string externalResourcesPath, string shareResourcesPath)
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

            headerView.OnChangeSelectCategoryAsObservable()
                .Subscribe(_ =>
                    {
                        manageAssetView.SetCategory(headerView.Category);
                    })
                .AddTo(Disposable);

            // ManageAssetView.

            manageAssetView = new ManageAssetView();

            manageAssetView.Initialize(assetManagement, externalResourcesPath, shareResourcesPath);

            manageAssetView.OnRequestRepaintAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(Disposable);

            manageAssetView.SetCategory(headerView.Category);
        }

        void OnDestroy()
        {
            if (AssetManagement.Prefs.manifestUpdateRequest)
            {
                AssetInfoManifestGenerator.Generate();
            }

            Disposable.Dispose();
        }

        void OnGUI()
        {
            headerView.DrawGUI();
            
            if (headerView.CategoryNameEdit)
            {
                EditorGUILayout.HelpBox("Can not operate while entering name.", MessageType.Warning);
                return;
            }
            else
            {
                if (string.IsNullOrEmpty(headerView.Category))
                {
                    EditorGUILayout.HelpBox("Please select category.", MessageType.Info);
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

        private void UpdateDragAndDrop()
        {
            var e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    var validate = false;

                    if (!string.IsNullOrEmpty(headerView.Category))
                    {
                        if (headerView.Category == ExternalResources.ShareCategoryName)
                        {
                            validate = assetManagement.IsShareResourcesTarget(DragAndDrop.objectReferences);
                        }
                        else
                        {
                            validate = assetManagement.IsExternalResourcesTarget(DragAndDrop.objectReferences);
                        }
                    }

                    DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;

                        if (validate)
                        {
                            OnDragAndDrop(DragAndDrop.objectReferences);
                        }
                    }

                    break;
            }
        }

        public void OnDragAndDrop(Object[] assetObjects)
        {
            if (string.IsNullOrEmpty(headerView.Category)) { return; }

            var assetObject = assetObjects.FirstOrDefault();

            if (assetObject == null) { return; }

            if (!assetManagement.ValidateManageInfo(assetObject)) { return; }
            
            // 管理情報を追加.
            assetManagement.AddManageInfo(headerView.Category, assetObject);

            manageAssetView.BuildManageInfoViews();

            Repaint();
        }
    }
}
