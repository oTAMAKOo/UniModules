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

            using (new EditorGUILayout.HorizontalScope())
            {
                var categories = Enum.GetValues(typeof(GameTextCategory)).Cast<GameTextCategory>().OrderBy(x => (int)x).ToList();

                var categoryIndex = categories.FindIndex(x => x == instance.Category);

                var categoryLabels = categories.Select(x => x.ToLabelName());
                var labels = categoryLabels.ToArray();

                var currentCategoryIndex = EditorGUILayout.Popup("GameText", categoryIndex, labels);

                if (categoryIndex != currentCategoryIndex)
                {
                    UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);
                    categoryIndex = currentCategoryIndex;

                    instance.SetCategory(categories[categoryIndex]);
                }

                using (new DisableScope(categoryIndex == 0 || GameText.Instance.Cache == null))
                {
                    GUILayout.Space(2f);
                    
                    if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(75f)))
                    {
                        GameTextSelector.Open();
                    }
                }
            }

            GUILayout.Space(2f);
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
