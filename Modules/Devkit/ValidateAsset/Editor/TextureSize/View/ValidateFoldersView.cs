
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.ValidateAsset.TextureSize
{
    public sealed partial class TextureSizeValidateConfigInspector
    {
        public sealed class ValidateFoldersView
        {
            //----- params -----

            //----- field -----

            private TextureSizeValidateConfigInspector inspector = null;

            private DisplayMode displayMode = DisplayMode.Asset;

            private ReorderableList reorderableList = null;

            private bool hasChanged = false;

            //----- property -----

            //----- method -----

            public void Initialize(TextureSizeValidateConfigInspector inspector)
            {
                this.inspector = inspector;

                SetupReorderableList();
            }

            public void DrawInspectorGUI()
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    displayMode = (DisplayMode)EditorGUILayout.EnumPopup(displayMode, GUILayout.Width(80f));

                    GUILayout.Space(5f);

                    if (GUILayout.Button("Sort by AssetPath", EditorStyles.miniButton, GUILayout.Width(125f)))
                    {
                        inspector.contents = inspector.contents
                            .OrderBy(x => AssetDatabase.GUIDToAssetPath(x.folderGuid), new NaturalComparer())
                            .ToList();

                        hasChanged = true;
                    }
                }

                EditorGUILayout.Separator();

                reorderableList.DoLayoutList();

                EditorLayoutTools.ContentTitle("Manage");

                using (new ContentsScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Ignore Assets", EditorStyles.miniButton))
                        {
                            inspector.viewType = ViewType.ManageIgnore;

                            if (hasChanged)
                            {
                                inspector.SaveContents();
                                hasChanged = false;
                            }
                        }

                        GUILayout.Space(5f);

                        if (GUILayout.Button("Validate", EditorStyles.miniButton))
                        {
                            inspector.viewType = ViewType.Validate;

                            if (hasChanged)
                            {
                                inspector.SaveContents();
                                hasChanged = false;
                            }
                        }
                    }
                }
            }

            private void SetupReorderableList()
            {
                if (reorderableList != null){ return; }

                reorderableList = new ReorderableList(new List<ValidateData>(), typeof(ValidateData));

                // ヘッダー描画コールバック.
                reorderableList.drawHeaderCallback = r =>
                {
                    EditorGUI.LabelField(r, "Target Folders");
                };

                // 要素描画コールバック.
                reorderableList.drawElementCallback = (r, index, isActive, isFocused) => 
                {
                    r.position = Vector.SetY(r.position, r.position.y + 2f);
                    r.height = EditorGUIUtility.singleLineHeight;

                    var content = inspector.contents.ElementAtOrDefault(index);

                    EditorGUI.BeginChangeCheck();

                    content = DrawContent(r, index, content);

                    if (EditorGUI.EndChangeCheck())
                    {
                        inspector.contents[index] = content;

                        reorderableList.list = inspector.contents;
                    }
                };

                // 順番入れ替えコールバック.
                reorderableList.onReorderCallback = x =>
                {
                    inspector.contents = x.list.Cast<ValidateData>().ToList();
                
                    UpdateContents();
                };

                // 追加コールバック.
                reorderableList.onAddCallback = list =>
                {
                    var newItem = new ValidateData();

                    inspector.contents.Add(newItem);

                    UpdateContents();
                };

                // 削除コールバック.
                reorderableList.onRemoveCallback = list =>
                {
                    inspector.contents.RemoveAt(list.index);

                    UpdateContents();
                };
            }

            private ValidateData DrawContent(Rect rect, int index, ValidateData data)
            {
                var totalWidth = rect.width;

                var ParameterWidth = 120f;
                var padding = 5f;

                EditorGUI.BeginChangeCheck();

                var folderGuid = data.folderGuid;

                switch (displayMode)
                {
                    case DisplayMode.Asset:
                        {
                            var folderAsset = UnityEditorUtility.FindMainAsset(folderGuid);

                            rect.width = totalWidth - (ParameterWidth + padding);

                            folderAsset = EditorGUI.ObjectField(rect, folderAsset, typeof(Object), false);

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (folderAsset != null)
                                {
                                    var isFolder = UnityEditorUtility.IsFolder(folderAsset);

                                    if (isFolder)
                                    {
                                        var newfolderGuid = UnityEditorUtility.GetAssetGUID(folderAsset);

                                        // フォルダ登録.
                                        if (inspector.contents.All(x => x.folderGuid != newfolderGuid))
                                        {
                                            inspector.contents[index].folderGuid = newfolderGuid;
                                        }
                                        // 既に登録済み.
                                        else
                                        {
                                            EditorUtility.DisplayDialog("Error", "This folder is already registered.", "close");
                                        }
                                    }
                                    // フォルダではない.
                                    else
                                    {
                                        EditorUtility.DisplayDialog("Error", "This asset is not a folder.", "close");
                                    }
                                }
                                // 設定が対象が外れた場合は初期化.
                                else
                                {
                                    data.folderGuid = string.Empty;
                                }

                                hasChanged = true;
                            }
                        }
                        break;

                    case DisplayMode.Path:
                        {
                            var assetPath = AssetDatabase.GUIDToAssetPath(folderGuid);

                            rect.width = totalWidth - (ParameterWidth + padding);

                            EditorGUI.TextField(rect, assetPath);
                        }
                        break;
                }
                
                using (new DisableScope(string.IsNullOrEmpty(data.folderGuid)))
                {
                    using (new LabelWidthScope(16f))
                    {
                        EditorGUI.BeginChangeCheck();

                        rect.x += rect.width + padding;
                        rect.width = 55f;
                        
                        data.width = EditorGUI.IntField(rect, "W", data.width);

                        rect.x += rect.width + padding;
                        rect.width = 55f;

                        data.heigth = EditorGUI.IntField(rect, "H", data.heigth);

                        if(EditorGUI.EndChangeCheck())
                        {
                            hasChanged = true;
                        }
                    }
                }

                return data;
            }

            public void UpdateContents()
            {
                reorderableList.list = inspector.contents;

                hasChanged = true;
            }
        }
    }
}