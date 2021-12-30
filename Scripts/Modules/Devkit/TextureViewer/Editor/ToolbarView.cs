
using UnityEngine;
using System;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using UnityEditor;

namespace Modules.Devkit.TextureViewer
{
    public sealed class ToolbarView
    {
        //----- params -----

        private static readonly BuildTargetGroup[] TargetPlatforms = new BuildTargetGroup[]
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
        };

        //----- field -----

        public BuildTargetGroup platform = default;

        public DisplayMode displayMode  = default;

        private Subject<BuildTargetGroup> onChangePlatform = null;

        private Subject<DisplayMode> onChangeDisplayMode = null;

        private Subject<Unit> onRequestSortReset = null;

        private Subject<string> onUpdateSearchText = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        public string SearchText { get; private set; }

        //----- method -----

        public void Initialize(BuildTargetGroup platform)
        {
            if (initialized) { return; }

            this.platform = platform;

            initialized = true;
        }

        public void DrawGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                DrawSelectPlatformGUI();

                GUILayout.Space(10f);

                DrawModeTabGUI();

                GUILayout.FlexibleSpace();

                DrawSortResetGUI();

                GUILayout.Space(10f);

                DrawSearchTextGUI();
            }
        }

        private void DrawSelectPlatformGUI()
        {
            if (TargetPlatforms.All(x => x != platform)){ return; }

            var selection = TargetPlatforms.IndexOf(x => x == platform);
            var labels = TargetPlatforms.Select(x => x.ToString()).ToArray();

            EditorGUI.BeginChangeCheck();

            selection = EditorGUILayout.Popup(selection, labels, EditorStyles.toolbarPopup, GUILayout.Width(95f));

            if (EditorGUI.EndChangeCheck())
            {
                if (selection != -1)
                {
                    platform = TargetPlatforms.ElementAt(selection);

                    if (onChangePlatform != null)
                    {
                        onChangePlatform.OnNext(platform);
                    }
                }
            }
        }

        private void DrawModeTabGUI()
        {
            var tabNames = Enum.GetNames(typeof(DisplayMode));
            var tabValues = Enum.GetValues(typeof(DisplayMode)).Cast<DisplayMode>().ToArray();

            var index = tabValues.IndexOf(x => x == displayMode);
            
            EditorGUI.BeginChangeCheck();

            index = GUILayout.Toolbar(index, tabNames, new GUIStyle(EditorStyles.toolbarButton), GUI.ToolbarButtonSize.FitToContents);

            if (EditorGUI.EndChangeCheck())
            {
                displayMode = tabValues.ElementAt(index);
                
                if (onChangeDisplayMode != null)
                {
                    onChangeDisplayMode.OnNext(displayMode);
                }
            }
        }

        private void DrawSortResetGUI()
        {
            if (GUILayout.Button("Sort Reset", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                if (onRequestSortReset != null)
                {
                    onRequestSortReset.OnNext(Unit.Default);
                }
            }
        }

        private void DrawSearchTextGUI()
        {
            Action<string> onChangeSearchText = x =>
            {
                SearchText = x;

                if (onUpdateSearchText != null)
                {
                    onUpdateSearchText.OnNext(SearchText);
                }
            };

            Action onSearchCancel = () =>
            {
                SearchText = string.Empty;

                if (onUpdateSearchText != null)
                {
                    onUpdateSearchText.OnNext(SearchText);
                }
            };

            SearchText = EditorLayoutTools.DrawToolbarSearchTextField(SearchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250));
        }

        public IObservable<BuildTargetGroup> OnChangePlatformAsObservable()
        {
            return onChangePlatform ?? (onChangePlatform = new Subject<BuildTargetGroup>());
        }

        public IObservable<DisplayMode> OnChangeDisplayModeAsObservable()
        {
            return onChangeDisplayMode ?? (onChangeDisplayMode = new Subject<DisplayMode>());
        }

        public IObservable<Unit> OnRequestSortResetAsObservable()
        {
            return onRequestSortReset ?? (onRequestSortReset = new Subject<Unit>());
        }

        public IObservable<string> OnUpdateSearchTextAsObservable()
        {
            return onUpdateSearchText ?? (onUpdateSearchText = new Subject<string>());
        }
    }
}