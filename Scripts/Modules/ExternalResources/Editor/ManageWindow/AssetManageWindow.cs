﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.CompileNotice;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    public class AssetManageWindow : SingletonEditorWindow<AssetManageWindow>
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(500f, 450f);

        //----- field -----

        private ManageAssetView manageAssetView = null;

        private AssetManageModel assetManageModel = null;
        private AssetManageManager assetManageManager = null;

        //----- property -----

        //----- method -----

        public static void Open(string externalResourcesPath)
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("Error", "Can not open while compiling.", "OK");
                return;
            }
            
            var assetManageConfig = AssetManageConfig.Instance;

            Instance.minSize = WindowSize;
            Instance.titleContent = new GUIContent("AssetManageWindow");

            Instance.Initialize(externalResourcesPath, assetManageConfig);

            Instance.ShowUtility();
        }

        private void Initialize(string externalResourcesPath, AssetManageConfig assetManageConfig)
        {
            // コンパイルが始まったら閉じる.
            CompileNotification.OnCompileStartAsObservable()
                .Subscribe(_ => Close())
                .AddTo(Disposable);

            assetManageConfig.Optimisation();

            assetManageModel = new AssetManageModel();
            assetManageModel.Initialize();

            assetManageManager = AssetManageManager.Instance;
            assetManageManager.Initialize(externalResourcesPath, assetManageConfig);

            manageAssetView = new ManageAssetView();
            manageAssetView.Initialize(assetManageModel, assetManageManager);
            
            assetManageModel.OnRequestRepaintAsObservable().Subscribe(_ => Repaint()).AddTo(Disposable);
        }

        void OnDestroy()
        {
            Disposable.Dispose();
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbarButton))
            {
                manageAssetView.DrawHeaderGUI();
            }

            EditorGUILayout.Separator();

            manageAssetView.DrawGUI();

            EditorGUILayout.Separator();

            UpdateDragAndDrop();
        }

        private void UpdateDragAndDrop()
        {
            var e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    var validate = assetManageManager.IsExternalResourcesTarget(DragAndDrop.objectReferences);

                    DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;

                        if (validate)
                        {
                            assetManageModel.DragAndDrop(DragAndDrop.objectReferences);
                        }
                    }

                    break;
            }
        }
    }
}
