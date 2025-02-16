
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.TextData.Components
{
    public sealed class TextDataValidationWindow : SingletonEditorWindow<TextDataValidationWindow>
    {
        //----- params -----

        private const string WindowTitle = "TextData Validation";

        private static readonly Color TitleColor = new Color(1.0f, 0.2f, 0.2f);

        public sealed class ValidateInfo
        {
            public string category = null;

            public string enumName = null;

            public string text = null;
        }

        //----- field -----

        private TextDataValidator validator = null;

        private TextDataAsset[] textDataAssets = null;

        private Vector2 listScrollPosition = Vector2.zero;

        private Dictionary<string, ValidateInfo[]> validateInfos = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        public void SetValidator(TextDataValidator validator)
        {
            this.validator = validator;
        }

        private void Initialize()
        {
            titleContent = new GUIContent(WindowTitle);

            validateInfos = new Dictionary<string, ValidateInfo[]>();

            Show(true);
        }

        void OnGUI()
        {
            if (validator == null)
            {
                SetValidator(new TextDataValidator());
            }

            if (textDataAssets == null || validateInfos == null)
            {
                LoadAllTextAssets();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                {
                    LoadAllTextAssets();
                }
            }

            using (new ContentsScope())
            {
                if (validateInfos.Any())
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(listScrollPosition))
                    {
                        foreach (var info in validateInfos)
                        {
                            EditorLayoutTools.Title(info.Key, TitleColor);

                            using (new ContentsScope())
                            {
                                foreach (var item in info.Value)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.SelectableLabel(item.category, EditorStyles.textArea, GUILayout.Width(150f), GUILayout.Height(18f));
                                        EditorGUILayout.SelectableLabel(item.enumName, EditorStyles.textArea, GUILayout.Width(300f), GUILayout.Height(18f));
                                        EditorGUILayout.SelectableLabel(item.text, EditorStyles.textArea, GUILayout.ExpandWidth(true), GUILayout.Height(18f));
                                    }
                                }
                            }
                        }

                        listScrollPosition = scrollViewScope.scrollPosition;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("InValid text not found", MessageType.Info);
                }
            }
        }

        private void LoadAllTextAssets()
        {
            textDataAssets = UnityEditorUtility.FindAssetsByType<TextDataAsset>($"t:{typeof(TextDataAsset).FullName}").ToArray();

            validateInfos = new Dictionary<string, ValidateInfo[]>();

            foreach (var textDataAsset in textDataAssets)
            {
                var inValidData = validator.Validation(textDataAsset);

                if (inValidData.Any())
                {
                    var assetPath = AssetDatabase.GetAssetPath(textDataAsset);

                    var infos = new List<ValidateInfo>();

                    foreach (var data in inValidData)
                    {
                        var categoryInfo = textDataAsset.Contents.FirstOrDefault(x => x.Guid == data.categoryGuid);
                        var textInfo = categoryInfo.Texts.FirstOrDefault(x => x.Guid == data.textGuid);

                        var info = new ValidateInfo()
                        {
                            category = categoryInfo.Name,
                            enumName = textInfo.EnumName,
                            text = textInfo.Text,
                        };

                        infos.Add(info);
                    }

                    validateInfos.Add(assetPath, infos.ToArray());
                }
            }
        }
    }
}