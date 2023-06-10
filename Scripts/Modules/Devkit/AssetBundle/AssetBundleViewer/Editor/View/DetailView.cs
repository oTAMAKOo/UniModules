
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.AssetBundles;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetBundleViewer
{
    public sealed class DetailView : LifetimeDisposable
    {
        //----- params -----

        private enum AssetDisplayType
        {
            Path,
            Asset,
        }

        //----- field -----

        private InfoTreeView infoTreeView = null;

        private AssetBundleInfo target = null;

        private Dictionary<string, AssetBundleInfo> assetBundleInfos = null;

        private AssetBundleDependencies assetBundleDependencies = null;

        private Vector2 scrollPosition = Vector2.zero;

        private Dictionary<string, string> assetPathCache = null;
        private Dictionary<string, Object> loadedAssetCache = null;

        private Stack<AssetBundleInfo> history = null;

        private AssetDisplayType assetDisplayType = AssetDisplayType.Asset;

        private Subject<Unit> onRequestClose = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public void Initialize(Dictionary<string, AssetBundleInfo> assetBundleInfos, AssetBundleDependencies assetBundleDependencies)
        {
            if (initialized) { return; }

            this.assetBundleInfos = assetBundleInfos;
            this.assetBundleDependencies = assetBundleDependencies;

            assetPathCache = new Dictionary<string, string>();
            loadedAssetCache = new Dictionary<string, Object>();

            history = new Stack<AssetBundleInfo>();

            //------ InfoTreeView ------

            infoTreeView = new InfoTreeView();

            infoTreeView.Initialize();

            infoTreeView.OnContentClickAsObservable()
                .Subscribe(x => SetContent(x))
                .AddTo(Disposable);

            initialized = true;
        }

        public void DrawGUI()
        {
            if (target == null){ return; }

            DrawToolBar();
            DrawInfo();
            DrawContainsAssets();
            DrawDependencies();
        }

        private void DrawToolBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                if (GUILayout.Button("Back", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                {
                    // remove current.
                    history.Pop();

                    if (history.Any())
                    {
                        var prev = history.Pop();

                        SetContent(prev);
                    }
                    else
                    {
                        if (onRequestClose != null)
                        {
                            onRequestClose.OnNext(Unit.Default);
                        }
                    }
                }

                GUILayout.FlexibleSpace();
                
                assetDisplayType = (AssetDisplayType)EditorGUILayout.EnumPopup(assetDisplayType, EditorStyles.toolbarPopup, GUILayout.Width(75f));
            }
        }

        private void DrawInfo()
        {
            EditorLayoutTools.Title("Info");

            using(new ContentsScope())
            {
                var labelWidth = 135f;
                var singleLineHeightLayoutOption = GUILayout.Height(EditorGUIUtility.singleLineHeight);

                void DrawInfoContent(string label, string content)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new LabelWidthScope(labelWidth))
                        {
                            EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth), singleLineHeightLayoutOption);
                        }

                        EditorGUILayout.SelectableLabel(content, singleLineHeightLayoutOption);
                    }

                    GUILayout.Space(2f);
                }

                DrawInfoContent("AssetBundleName", target.AssetBundleName);
                DrawInfoContent("Group", target.Group);
                DrawInfoContent("Labels", target.GetLabelsText());
                DrawInfoContent("FileName", target.FileName);
                DrawInfoContent("FileSize", ByteDataUtility.GetBytesReadable(target.FileSize));
                DrawInfoContent("LoadFileSize", ByteDataUtility.GetBytesReadable(target.LoadFileSize));
            }
        }

        private void DrawContainsAssets()
        {
            EditorLayoutTools.Title($"Assets ({target.GetAssetCount()})");

            using (new ContentsScope())
            {
                var singleLineHeightLayoutOption = GUILayout.Height(EditorGUIUtility.singleLineHeight);

                var dependenciesCount = target.GetDependenciesCount();

                var hightLayoutOption = 0 < dependenciesCount ? GUILayout.Height(200) : GUILayout.ExpandHeight(true);

                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.ExpandWidth(true), hightLayoutOption))
                {
                    foreach (var assetGuid in target.AssetGuids)
                    {
                        if (!loadedAssetCache.ContainsKey(assetGuid))
                        {
                            assetPathCache[assetGuid] = AssetDatabase.GUIDToAssetPath(assetGuid);
                            loadedAssetCache[assetGuid] = UnityEditorUtility.FindMainAsset(assetGuid);
                        }

                        switch (assetDisplayType)
                        {
                            case AssetDisplayType.Asset:
                                EditorLayoutTools.ObjectField(loadedAssetCache[assetGuid], false);
                                break;
                            case AssetDisplayType.Path:
                                EditorGUILayout.SelectableLabel(assetPathCache[assetGuid], singleLineHeightLayoutOption);
                                break;
                        }
                    }

                    scrollPosition = scrollView.scrollPosition;
                }
            }
        }

        private void DrawDependencies()
        {
            var dependenciesCount = target.GetDependenciesCount();

            if (0 < dependenciesCount)
            {
                EditorLayoutTools.Title($"AssetBundle Dependencies ({dependenciesCount})");

                GUILayout.Space(2f);

                infoTreeView.DrawGUI();
            }
        }

        public void SetContent(AssetBundleInfo target)
        {
            this.target = target;

            scrollPosition = Vector2.zero;

            var dependencies = new List<AssetBundleInfo>();

            var allDependencies = assetBundleDependencies.GetAllDependencies(target.AssetBundleName);

            foreach (var item in allDependencies)
            {
                var content = assetBundleInfos.GetValueOrDefault(item);

                if (content == null) { continue; }

                dependencies.Add(content);
            }

            infoTreeView.SetContents(dependencies.ToArray());

            history.Push(target);
        }
        
        public IObservable<Unit> OnRequestCloseAsObservable()
        {
            return onRequestClose ?? (onRequestClose = new Subject<Unit>());
        }
    }
}