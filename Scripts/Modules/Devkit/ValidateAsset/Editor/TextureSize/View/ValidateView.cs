
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.ValidateAsset.TextureSize
{
    public sealed partial class TextureSizeValidateConfigInspector
    {
        public sealed class ValidateView
        {
            //----- params -----

            //----- field -----

            private TextureSizeValidateConfigInspector inspector = null;

            private ValidateTextureSize validateTextureSize = null;

            private DisplayMode displayMode = DisplayMode.Asset;

            private ValidateTextureSize.ValidateResult[] results = null;
            
            private Vector2 scrollPosition = Vector2.zero;

            private Dictionary<string, Object> folderAssetByGuid = null;

            private bool running = false;

            //----- property -----

            //----- method -----

            public void Initialize(TextureSizeValidateConfigInspector inspector)
            {
                this.inspector = inspector;
                
                validateTextureSize = inspector.validateTextureSize;

                folderAssetByGuid = new Dictionary<string, Object>();
            }

            public void DrawInspectorGUI()
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    using (new DisableScope(running))
                    {
                        if (GUILayout.Button("close", EditorStyles.miniButton, GUILayout.Width(65f)))
                        {
                            inspector.viewType = ViewType.ValidateFolders;
                        }
                    }
                }

                EditorGUILayout.Separator();

                using (new DisableScope(running))
                {
                    if (GUILayout.Button("Start"))
                    {
                        ExecuteValidate();
                    }
                }

                if (results != null)
                {
                    EditorGUILayout.Separator();

                    EditorLayoutTools.ContentTitle("Result");

                    using (new ContentsScope())
                    {
                        displayMode = (DisplayMode)EditorGUILayout.EnumPopup(displayMode, GUILayout.Width(80f));

                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                        {
                            foreach (var item in results)
                            {
                                if (item.violationTextures.IsEmpty()){ continue; }

                                using (new ContentsScope())
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        var validateData = item.validateData;

                                        EditorGUILayout.LabelField($"{validateData.width}x{validateData.heigth}", GUILayout.Width(85f));

                                        GUILayout.Space(2f);

                                        var folder = folderAssetByGuid.GetValueOrDefault(item.validateData.folderGuid);

                                        if (folder == null)
                                        {
                                            folder = UnityEditorUtility.FindMainAsset(item.validateData.folderGuid);

                                            if (folder != null)
                                            {
                                                folderAssetByGuid[item.validateData.folderGuid] = folder;
                                            }
                                        }

                                        switch (displayMode)
                                        {
                                            case DisplayMode.Asset:
                                                EditorGUILayout.ObjectField(folder, typeof(Object), false, GUILayout.ExpandWidth(true));
                                                break;

                                            case DisplayMode.Path:
                                                EditorGUILayout.TextField(AssetDatabase.GetAssetPath(folder), GUILayout.ExpandWidth(true));
                                                break;
                                        }
                                    }

                                    var originIndentLevel = EditorGUI.indentLevel;

                                    EditorGUI.indentLevel++;

                                    foreach (var violationTexture in item.violationTextures)
                                    {
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            EditorGUILayout.LabelField($"{violationTexture.width}x{violationTexture.height}", GUILayout.Width(85f));

                                            GUILayout.Space(2f);

                                            switch (displayMode)
                                            {
                                                case DisplayMode.Asset:
                                                    EditorGUILayout.ObjectField(violationTexture, typeof(Texture), false, GUILayout.ExpandWidth(true));
                                                    break;

                                                case DisplayMode.Path:
                                                    EditorGUILayout.TextField(AssetDatabase.GetAssetPath(violationTexture), GUILayout.ExpandWidth(true));
                                                    break;
                                            }
                                        }
                                    }

                                    EditorGUI.indentLevel = originIndentLevel;
                                }

                                GUILayout.Space(2f);
                            }

                            scrollPosition = scrollViewScope.scrollPosition;
                        }
                    }
                }
            }

            private void ExecuteValidate()
            {
                running = true;

                try
                {
                    results = validateTextureSize.Validate();
                }
                finally
                {
                    running = false;
                }
            }
        }
    }
}