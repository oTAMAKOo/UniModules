
#if ENABLE_CRIWARE_ADX

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;
using Modules.CriWare;
using Modules.CriWare.Editor;
using Modules.Devkit.Project;

namespace Modules.SoundManagement.Editor
{
    public class CueNavigationWindow : SingletonEditorWindow<CueNavigationWindow>
    {
        //----- params -----

        private static class Prefs
        {
            public static string resourceDir
            {
                get { return ProjectPrefs.GetString("CueNavigationWindow-Prefs-resourceDir"); }
                set { ProjectPrefs.SetString("CueNavigationWindow-Prefs-resourceDir", value); }
            }

            public static string selectionAssetGUID
            {
                get { return ProjectPrefs.GetString("CueNavigationWindow-Prefs-selectionAssetGUID"); }
                set { ProjectPrefs.SetString("CueNavigationWindow-Prefs-selectionAssetGUID", value); }
            }
        }

        //----- field -----

        private string resourceDir = null;
        private Object selectAcbAsset = null;
        private Vector2 scrollPosition = Vector2.zero;
        private List<CueInfo> cueInfos = null;
        private bool isLoaded = false;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            var editorConfig = ProjectFolders.Instance;

            var externalResourcesPath = editorConfig.ExternalResourcesPath;

            Instance.Initialize(externalResourcesPath);

            Instance.titleContent = new GUIContent("Cue Navigation");
            Instance.Show();
        }

        public void Initialize(string resourceDir)
        {
            if (initialized) { return; }

            this.resourceDir = resourceDir;

            Prefs.resourceDir = resourceDir;
            Prefs.selectionAssetGUID = null;

            initialized = true;
        }

        void OnEnable()
        {
            if (!Instance.Reload())
            {
                Instance.Close();
            }
        }

        void OnGUI()
        {
            var backgroundColor = new Color(0.3f, 0.5f, 0.3f);

            EditorGUILayout.Separator();

            var acbPath = selectAcbAsset != null ? AssetDatabase.GetAssetPath(selectAcbAsset) : null;

            if (!string.IsNullOrEmpty(acbPath))
            {
                EditorLayoutTools.DrawLabelWithBackground("CueSheet", backgroundColor);

                GUILayout.Space(5f);

                EditorGUILayout.ObjectField(selectAcbAsset, typeof(Object), false);

                EditorGUILayout.Separator();

                if (acbPath.StartsWith(resourceDir))
                {
                    var resourcePath = string.Empty;

                    resourcePath = UnityPathUtility.GetLocalPath(acbPath, resourceDir);
                    resourcePath = PathUtility.GetPathWithoutExtension(resourcePath);

                    EditorLayoutTools.DrawLabelWithBackground("Resource Path", backgroundColor);

                    GUILayout.Space(5f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("copy", GUILayout.Width(40f)))
                        {
                            GUIUtility.systemCopyBuffer = resourcePath;
                        }

                        EditorGUILayout.SelectableLabel(resourcePath, EditorStyles.textArea, GUILayout.Height(18f));
                    }
                }
            }

            if (string.IsNullOrEmpty(acbPath))
            {
                EditorGUILayout.HelpBox("Drag and drop Acb asset.", MessageType.Info);
            }

            EditorGUILayout.Separator();

            if (!string.IsNullOrEmpty(acbPath))
            {
                if (isLoaded && cueInfos != null)
                {
                    if (cueInfos.IsEmpty())
                    {
                        EditorGUILayout.HelpBox("CueSheet dont have Cue.", MessageType.Warning);
                    }
                    else
                    {
                        EditorLayoutTools.DrawLabelWithBackground("Cue", backgroundColor);

                        GUILayout.Space(5f);

                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                        {
                            foreach (var cueInfo in cueInfos)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    if (GUILayout.Button("copy", GUILayout.Width(40f)))
                                    {
                                        GUIUtility.systemCopyBuffer = cueInfo.Cue;
                                    }

                                    GUIContent contentText = null;

                                    if (string.IsNullOrEmpty(cueInfo.Summary))
                                    {
                                        contentText = new GUIContent(cueInfo.Cue);
                                    }
                                    else
                                    {
                                        var tooltip = string.Format("[{0}]\n{1}", cueInfo.Cue, cueInfo.Summary);
                                        contentText = new GUIContent(cueInfo.Cue, tooltip);
                                    }

                                    GUILayout.Label(contentText, GUILayout.Height(18f));
                                }

                                GUILayout.Space(2f);
                            }

                            scrollPosition = scrollViewScope.scrollPosition;
                        }
                    }
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Separator();

            UpdateDragAndDrop();
        }

        private void LoadCueInfo(Object acbAsset)
        {
            cueInfos = new List<CueInfo>();

            // 指定したACBファイル名(キューシート名)を指定してキュー情報を取得.
            var assetPath = AssetDatabase.GetAssetPath(acbAsset);
            var fullPath = UnityPathUtility.GetProjectFolderPath() + assetPath;
            var acb = CriAtomExAcb.LoadAcbFile(null, fullPath, "");

            if (acb != null)
            {
                var list = acb.GetCueInfoList();

                foreach (var item in list)
                {
                    cueInfos.Add(new CueInfo(item.name, PathUtility.GetPathWithoutExtension(assetPath), item.userData));
                }

                acb.Dispose();
            }

            isLoaded = true;
        }

        private bool Reload()
        {
            if (string.IsNullOrEmpty(Prefs.resourceDir)) { return false; }

            resourceDir = Prefs.resourceDir;
            isLoaded = false;

            if (!string.IsNullOrEmpty(Prefs.selectionAssetGUID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(Prefs.selectionAssetGUID);
                selectAcbAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

                if (selectAcbAsset != null)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        // CriWareInitializerの初期化を待つ.
                        Observable.EveryUpdate()
                            .SkipWhile(_ => !CriWareInitializer.IsInitialized())
                            .First()
                            .Subscribe(
                                _ =>
                                {
                                    LoadCueInfo(selectAcbAsset);
                                    Repaint();
                                })
                            .AddTo(Disposable);
                    }
                    else
                    {
                        CriForceInitializer.Initialize();
                        
                        LoadCueInfo(selectAcbAsset);
                    }
                }
            }

            return true;
        }

        private void UpdateDragAndDrop()
        {
            var e = Event.current;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    var assetObject = DragAndDrop.objectReferences.FirstOrDefault();

                    if(assetObject == null) { return; }

                    var assetPath = AssetDatabase.GetAssetPath(assetObject);
                    var validate = Path.GetExtension(assetPath) == CriAssetDefinition.AcbExtension;

                    DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;

                        if (validate)
                        {
                            selectAcbAsset = assetObject;

                            CriForceInitializer.Initialize();
                            LoadCueInfo(selectAcbAsset);

                            Prefs.selectionAssetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                        }
                    }

                    break;
            }
        }
    }
}

#endif
