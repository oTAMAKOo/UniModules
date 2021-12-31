
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.MasterViewer
{
    public sealed class ToolbarView
    {
        //----- params -----

        //----- field -----

        private MasterController masterController = null;

        private Subject<Unit> onUpdateSearchText = null;

        private Subject<Unit> onResetRecords = null;

        [NonSerialized]
        private bool initialized = false;
        
        //----- property -----

        public string SearchText { get; private set; }

        //----- method -----

        public void Initialize(MasterController masterController)
        {
            if (initialized) { return; }

            this.masterController = masterController;

            initialized = true;
        }

        public void DrawGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                DrawRecordRevertGUI();

                GUILayout.Space(10f);

                DrawSearchTextGUI();
            }
        }

        private void DrawRecordRevertGUI()
        {
            var hasChanged = masterController.HasChangedRecord;

            using (new DisableScope(!hasChanged))
            {
                if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    masterController.ResetAll();

                    if (onResetRecords != null)
                    {
                        onResetRecords.OnNext(Unit.Default);
                    }
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
                    onUpdateSearchText.OnNext(Unit.Default);
                }
            };

            Action onSearchCancel = () =>
            {
                SearchText = string.Empty;

                if (onUpdateSearchText != null)
                {
                    onUpdateSearchText.OnNext(Unit.Default);
                }
            };

            SearchText = EditorLayoutTools.DrawToolbarSearchTextField(SearchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250));
        }

        public IObservable<Unit> OnChangeSearchTextAsObservable()
        {
            return onUpdateSearchText ?? (onUpdateSearchText = new Subject<Unit>());
        }

        public IObservable<Unit> OnResetRecordsAsObservable()
        {
            return onResetRecords ?? (onResetRecords = new Subject<Unit>());
        }
    }
}