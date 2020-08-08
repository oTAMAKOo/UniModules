
using UnityEngine;
using UnityEditor;
using System.Linq;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    public sealed class AssetNavigationWindow : SingletonEditorWindow<AssetNavigationWindow>
    {
        //----- params -----

        private static class Prefs
        {
            public static string selectionAssetGUID
            {
                get { return ProjectPrefs.GetString("AssetNavigationWindowPrefs-selectionAssetGUID"); }
                set { ProjectPrefs.SetString("AssetNavigationWindowPrefs-selectionAssetGUID", value); }
            }

            public static string externalResourcesPath
            {
                get { return ProjectPrefs.GetString("AssetNavigationWindowPrefs-externalResourcesPath"); }
                set { ProjectPrefs.SetString("AssetNavigationWindowPrefs-externalResourcesPath", value); }
            }
        }

		//----- field -----

		private AssetManagement assetManagement = null;

        private Object selectionAssetObject = null;

        private string assetCategory = null;
        private string assetBundleName = null;
        private string assetLoadPath = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open(string externalResourcesPath)
        {
			Instance.titleContent = new GUIContent("Asset Navigation");

            Instance.Initialize(externalResourcesPath);

            Instance.Show();
        }

        private void Initialize(string externalResourcesPath)
        {
            if (initialized) { return; }

			Prefs.externalResourcesPath = externalResourcesPath;

            ManageConfig.OnReloadAsObservable()
                .Subscribe(_ => Setup())
                .AddTo(Disposable);

            Setup();

			initialized = true;
        }

		private void Setup()
		{
			if (string.IsNullOrEmpty(Prefs.externalResourcesPath)) { return; }

			var externalResourcesPath = Prefs.externalResourcesPath;

            assetManagement = AssetManagement.Instance;
			assetManagement.Initialize(externalResourcesPath);

			if (!string.IsNullOrEmpty(Prefs.selectionAssetGUID))
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(Prefs.selectionAssetGUID);
				selectionAssetObject = AssetDatabase.LoadMainAssetAtPath(assetPath);

				UpdateViewInfo(selectionAssetObject);
			}

			initialized = true;

			Repaint();
		}

		void OnEnable()
        {
            Setup();
        }

        void OnDestroy()
        {
            Prefs.externalResourcesPath = null;
            Prefs.selectionAssetGUID = null;
        }

        void Update()
        {
            if (!initialized)
            {
                Setup();
            }
        }

        void OnGUI()
        {
            if (!initialized) { return; }

            EditorGUILayout.Separator();

            if (selectionAssetObject != null)
            {
                EditorGUILayout.ObjectField(string.Empty, selectionAssetObject, typeof(Object), false, GUILayout.Width(250f));

                if (selectionAssetObject != null)
                {
                    GUILayout.Space(4f);

                    using (new ContentsScope())
                    {
                        if (!string.IsNullOrEmpty(assetCategory))
                        {
                            DrawContentGUI("GroupName", assetCategory);
                        }

                        if (!string.IsNullOrEmpty(assetBundleName))
                        {
                            DrawContentGUI("AssetBundleName", assetBundleName);
                        }

                        DrawContentGUI("LoadPath", assetLoadPath);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Drag and drop assetbundle asset.", MessageType.Info);
            }

            EditorGUILayout.Separator();

            UpdateDragAndDrop();
        }

        private void DrawContentGUI(string label, string content)
        {
            EditorLayoutTools.DrawLabelWithBackground(label, new Color(0.3f, 0.3f, 0.5f), new Color(0.8f, 0.8f, 0.8f, 0.8f));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    if (GUILayout.Button("copy", GUILayout.Width(45f), GUILayout.Height(18f)))
                    {
                        UniClipboard.SetText(content);
                    }

                    GUILayout.Space(2f);
                }

                var textStyle = new GUIStyle();
                var textSize = textStyle.CalcSize(new GUIContent(content));

                var originLabelWidth = EditorLayoutTools.SetLabelWidth(textSize.x);

                GUILayout.Label(content, GUILayout.Height(20f));

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                GUILayout.FlexibleSpace();
            }
        }

        private void UpdateDragAndDrop()
        {
            var e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    var validate = assetManagement.IsExternalResourcesTarget(DragAndDrop.objectReferences);

                    DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;

                        if (validate)
                        {
                            var assetObject = DragAndDrop.objectReferences.FirstOrDefault();

                            if (assetObject != null)
                            {
                                var enable = UpdateViewInfo(assetObject);

                                if (!enable)
                                {
                                    Debug.LogError("ExternalResourceの対象ではありません.");
                                }
                            }
                        }
                    }

                    break;
            }
        }

        private bool UpdateViewInfo(Object assetObject)
        {
            var assetPath = AssetDatabase.GetAssetPath(assetObject);

            var infos = assetManagement.GetAssetInfos(assetPath);

            var info = infos.FirstOrDefault();
            
            selectionAssetObject = info != null ? assetObject : null;
            assetCategory = info != null ? info.Category : null;
            assetBundleName = info != null && info.IsAssetBundle ? info.AssetBundle.AssetBundleName : null;
            assetLoadPath = string.IsNullOrEmpty(assetPath) ? string.Empty : assetManagement.GetAssetLoadPath(assetPath);

            if (info == null) { return false; }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            Prefs.selectionAssetGUID = guid;

            return true;
        }
    }
}
