
using UnityEngine;
using UnityEditor;
using System.Linq;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

using Object = UnityEngine.Object;

namespace Modules.ExternalAssets
{
    public sealed class AssetNavigationWindow : SingletonEditorWindow<AssetNavigationWindow>
    {
        //----- params -----

        private static class Prefs
        {
            public static string selectionAssetGUID
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-selectionAssetGUID"); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-selectionAssetGUID", value); }
            }
        }

		//----- field -----

		private AssetManagement assetManagement = null;

        private Object selectionAssetObject = null;

        private string assetGroup = null;
        private string assetBundleName = null;
        private string assetLoadPath = null;

        private GUIContent clipboardIcon = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open(string externalAssetPath)
        {
			Instance.titleContent = new GUIContent("Asset Navigation");

            Instance.Initialize();

            Instance.Show();
        }

        private void Initialize()
        {
            if (initialized) { return; }
            
            ManageConfig.OnReloadAsObservable()
                .Subscribe(_ => Setup())
                .AddTo(Disposable);

            Setup();

			initialized = true;
        }

		private void Setup()
		{
            assetManagement = AssetManagement.Instance;
			assetManagement.Initialize();

			if (!string.IsNullOrEmpty(Prefs.selectionAssetGUID))
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(Prefs.selectionAssetGUID);
				selectionAssetObject = AssetDatabase.LoadMainAssetAtPath(assetPath);

				UpdateViewInfo(selectionAssetObject);
			}

            clipboardIcon = EditorGUIUtility.IconContent("Clipboard");

            initialized = true;

			Repaint();
		}

		void OnEnable()
        {
            Setup();
        }

        void OnDestroy()
        {
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
                EditorGUILayout.ObjectField(string.Empty, selectionAssetObject, typeof(Object), false);

                if (selectionAssetObject != null)
                {
                    GUILayout.Space(4f);

                    if (!string.IsNullOrEmpty(assetGroup))
                    {
                        DrawContentGUI("Group", assetGroup);
                    }

                    if (!string.IsNullOrEmpty(assetBundleName))
                    {
                        DrawContentGUI("AssetBundleName", assetBundleName);
                    }

                    DrawContentGUI("LoadPath", assetLoadPath);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Drag and drop AssetBundle asset.", MessageType.Info);
            }

            EditorGUILayout.Separator();

            UpdateDragAndDrop();
        }

        private void DrawContentGUI(string label, string content)
        {
            EditorLayoutTools.Title(label);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Height(18f)))
                {
                    if (GUILayout.Button(clipboardIcon, GUILayout.Width(24f), GUILayout.Height(18f)))
                    {
                        GUIUtility.systemCopyBuffer = content;
                    }

                    GUILayout.Space(2f);
                }

                GUILayout.Space(-4f);

                using (new EditorGUILayout.VerticalScope(GUILayout.Height(18f)))
                {
                    GUILayout.Space(4f);

                    var textStyle = new GUIStyle();
                    var textSize = textStyle.CalcSize(new GUIContent(content));

                    var originLabelWidth = EditorLayoutTools.SetLabelWidth(textSize.x);

                    GUILayout.Label(content, GUILayout.Height(18f));

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(2f);
        }

        private void UpdateDragAndDrop()
        {
            var e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        var objectReferences = DragAndDrop.objectReferences;

                        var validate = false;
                        
                        validate |= assetManagement.IsExternalAssetTarget(objectReferences);
                        validate |= assetManagement.IsShareResourcesTarget(objectReferences);

                        DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                        if (e.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            DragAndDrop.activeControlID = 0;

                            if (validate)
                            {
                                var assetObject = objectReferences.FirstOrDefault();

                                if (assetObject != null)
                                {
                                    var enable = UpdateViewInfo(assetObject);

                                    if (!enable)
                                    {
                                        Debug.LogError("Not target of ExternalAsset.");
                                    }
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
            assetGroup = info != null ? info.Group : null;
            assetBundleName = info != null && info.IsAssetBundle ? info.AssetBundle.AssetBundleName : null;
            assetLoadPath = string.IsNullOrEmpty(assetPath) ? string.Empty : assetManagement.GetAssetLoadPath(assetPath);

            if (info == null) { return false; }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            Prefs.selectionAssetGUID = guid;

            return true;
        }
    }
}
