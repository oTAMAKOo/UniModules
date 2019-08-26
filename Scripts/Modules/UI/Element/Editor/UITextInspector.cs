﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Extensions.Serialize;
using Modules.GameText;
using Modules.GameText.Components;

namespace Modules.UI.Element
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UIText), true)]
    public class UITextInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private GameTextSetter gameTextSetter = null;

        private LifetimeDisposable lifetimeDisposable = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            var instance = target as UIText;

            lifetimeDisposable = new LifetimeDisposable();
            
            if (UnityUtility.IsNull(gameTextSetter))
            {
                gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(instance);

                if (gameTextSetter != null)
                {
                    gameTextSetter.ObserveEveryValueChanged(x => x.Category)
                        .TakeUntilDisable(instance)
                        .Subscribe(x => OnGameTextSetterCategoryChanged(x))
                        .AddTo(lifetimeDisposable.Disposable);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var instance = target as UIText;

            var selection = Reflection.GetPrivateField<UIText, IntNullable>(instance, "selection");
            var colorInfos = Reflection.GetPrivateProperty<UIText, UIText.TextColor[]>(instance, "ColorInfos");

            var infos = new UIText.TextColor[] { new UIText.TextColor() }.Concat(colorInfos).ToArray();
            var select = selection.HasValue ? infos.IndexOf(x => x.Type == selection.Value) : 0;

            var current = infos[select];

            using (new EditorGUILayout.HorizontalScope())
            {
                var labels = infos.Select(x => x.LabelName).ToArray();

                EditorGUI.BeginChangeCheck();

                select = EditorGUILayout.Popup("Text Color", select, labels);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("UITextInspector-Undo", instance);

                    current = infos[select];
                    instance.SetColor(current.Type);
                }
            }

            if (current != null)
            {
                if (current.ShadowColor.HasValue)
                {
                    EditorGUILayout.HelpBox("Shadow color exists", MessageType.Info);
                }

                if (current.OutlineColor.HasValue)
                {
                    EditorGUILayout.HelpBox("Outline color exists", MessageType.Info);
                }
            }

            using (new DisableScope(gameTextSetter != null && gameTextSetter.Category != GameTextCategory.None))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var labelWidth = EditorGUIUtility.labelWidth - 10f;

                    EditorGUILayout.LabelField("Development Text", GUILayout.Width(labelWidth));

                    var developmentText = instance.GetDevelopmentText().TrimStart(UIText.DevelopmentMark);

                    var prevText = developmentText;

                    EditorGUI.BeginChangeCheck();

                    developmentText = EditorGUILayout.TextArea(developmentText, GUILayout.ExpandWidth(true), GUILayout.Height(30f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("UITextInspector-Undo", instance);

                        if (!string.IsNullOrEmpty(prevText))
                        {
                            instance.text = null;
                        }

                        instance.SetDevelopmentText(developmentText);
                        instance.ImportText();
                    }
                }
            }
        }

        private void OnGameTextSetterCategoryChanged(GameTextCategory category)
        {
            var instance = target as UIText;

            if (instance == null) { return; }

            if (category != GameTextCategory.None)
            {
                instance.SetDevelopmentText(null);
                instance.text = null;
            }
        }
    }
}
