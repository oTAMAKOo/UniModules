﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.GameText.Components
{
    [CustomEditor(typeof(GameTextSetter))]
    public class GameTextSetterInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private GameTextSetter instance = null;

        //----- property -----

        public GameTextSetter Instance { get { return instance; } }

        public static GameTextSetterInspector Current { get; private set; }

        //----- method -----

        void OnEnable()
        {
            Current = this;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Current = null;
        }

        public override void OnInspectorGUI()
        {
            instance = target as GameTextSetter;

            EditorGUILayout.Separator();

            var categories = Enum.GetValues(typeof(GameTextCategory)).Cast<GameTextCategory>().OrderBy(x => (int)x).ToList();

            var categoryIndex = categories.FindIndex(x => x == instance.Category);

            var categoryLabels = categories.Select(x => x.ToLabelName());
            var currentCategoryIndex = EditorGUILayout.Popup("Category", categoryIndex, categoryLabels.ToArray());

            if (categoryIndex != currentCategoryIndex)
            {
                UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);
                categoryIndex = currentCategoryIndex;

                instance.SetCategory(categories[categoryIndex]);
            }

            if (categoryIndex != 0)
            {
                EditorGUILayout.Separator();

                if (EditorLayoutTools.DrawPrefixButton("Selection GameText", GUILayout.Width(180f)))
                {
                    GameTextSelector.Open();
                }

                GUILayout.Space(5f);

                if (EditorLayoutTools.DrawHeader("SourceText", "GameTextSetterInspector-SourceText"))
                {
                    EditorGUILayout.TextArea(instance.Content, GUILayout.Height(48f));
                }
            }

            EditorGUILayout.Separator();
        }

        private void OnUndoRedo()
        {
            if (instance != null)
            {
                instance.ImportText();
            }
        }
    }
}
