
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;
using Modules.Devkit.Inspector;

namespace Modules.Devkit.ValidateAsset.TextureSize
{
    public sealed partial class TextureSizeValidateConfigInspector
    {
        public sealed class ManageIgnoreView : LifetimeDisposable
        {
            //----- params -----

            private sealed class FolderNameRegisterScrollView : RegisterScrollView<string>
            {
                protected override string CreateNewContent()
                {
                    return string.Empty;
                }

                protected override string DrawContent(Rect rect, int index, string content)
                {
                    return EditorGUI.DelayedTextField(rect, content);
                }
            }

            //----- field -----

            private TextureSizeValidateConfigInspector inspector = null;

            private ValidateTextureSize validateTextureSize = null;

            private FolderNameRegisterScrollView ignoreFolderNameRegisterScrollView = null;

            private DisplayMode displayMode = DisplayMode.Asset;

            private List<Object> ignoreTargets = null;

            private Vector2 scrollPosition = Vector2.zero;

            //----- property -----

            //----- method -----

            public void Initialize(TextureSizeValidateConfigInspector inspector)
            {
                this.inspector = inspector;

                validateTextureSize = inspector.validateTextureSize;
            }

            public void DrawInspectorGUI()
            {
                if (ignoreTargets == null)
                {
                    ignoreTargets = new List<Object>();

                    var ignoreGuids = inspector.instance.GetIgnoreGuids();

                    foreach (var ignoreGuid in ignoreGuids)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(ignoreGuid);

                        var target = AssetDatabase.LoadMainAssetAtPath(assetPath);

                        if (target != null)
                        {
                            ignoreTargets.Add(target);
                        }
                    }
                }

                if (ignoreFolderNameRegisterScrollView == null)
                {
                    ignoreFolderNameRegisterScrollView = new FolderNameRegisterScrollView();

                    ignoreFolderNameRegisterScrollView.OnUpdateContentsAsObservable()
                        .Subscribe(x => Reflection.SetPrivateField(inspector.instance, "ignoreFolderNames", x))
                        .AddTo(Disposable);

                    var ignoreFolderNames = inspector.instance.GetIgnoreFolderNames().ToArray();

                    ignoreFolderNameRegisterScrollView.SetContents(ignoreFolderNames);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("close", EditorStyles.miniButton, GUILayout.Width(65f)))
                    {
                        ignoreTargets = null;
                        ignoreFolderNameRegisterScrollView = null;

                        inspector.viewType = ViewType.ValidateFolders;

                        inspector.SaveContents();
                    }
                }

                EditorGUILayout.Separator();

                var logBuilder = new StringBuilder();

                EditorLayoutTools.ContentTitle("Add Ignore Asset");

                var add = EditorLayoutTools.MultipleDragAndDrop<Object>("Drag & Drop", height: 60f);

                EditorGUILayout.Separator();

                EditorLayoutTools.ContentTitle("Remove Ignore Asset");

                var remove = EditorLayoutTools.MultipleDragAndDrop<Object>("Drag & Drop", height: 60f);

                if (add.Any() || remove.Any())
                {
                    using (new DisableStackTraceScope())
                    {
                        var targetFolderInfos = validateTextureSize.GetTargetFolderInfos();

                        var ignoreGuids = Reflection.GetPrivateField<TextureSizeValidateConfig, string[]>(inspector.instance, "ignoreGuids");

                        var list = ignoreGuids.ToList();

                        foreach (var target in add)
                        {
                            if (!(target is Texture || UnityEditorUtility.IsFolder(target))){ continue; }

                            var assetPath = AssetDatabase.GetAssetPath(target);

                            if (targetFolderInfos.Keys.All(x => !assetPath.StartsWith(x)))
                            {
                                Debug.LogError($"Unregistered folder asset.\n {assetPath}");
                                continue;
                            }

                            list.Add(AssetDatabase.AssetPathToGUID(assetPath));

                            logBuilder.AppendLine($"Add: {assetPath}");
                        }

                        foreach (var target in remove)
                        {
                            if (!(target is Texture || UnityEditorUtility.IsFolder(target))){ continue; }

                            var assetPath = AssetDatabase.GetAssetPath(target);

                            if (targetFolderInfos.Keys.All(x => !assetPath.StartsWith(x)))
                            {
                                Debug.LogError($"Unregistered folder asset.\n {assetPath}");
                                continue;
                            }

                            list.Remove(AssetDatabase.AssetPathToGUID(assetPath));

                            logBuilder.AppendLine($"Remove: {assetPath}");
                        }

                        ignoreGuids = list.Distinct()
                            .OrderBy(x => AssetDatabase.GUIDToAssetPath(x), new NaturalComparer())
                            .ToArray();

                        Reflection.SetPrivateField(inspector.instance, "ignoreGuids", ignoreGuids);

                        UnityEditorUtility.SaveAsset(inspector.instance);

                    
                        LogUtility.ChunkLog(logBuilder.ToString(), "Update Ignore", x => UnityConsole.Info(x));
                    }

                    ignoreTargets = null;
                }

                GUILayout.Space(2f);

                if (ignoreFolderNameRegisterScrollView != null)
                {
                    EditorLayoutTools.ContentTitle("Ignore FolderNames");

                    ignoreFolderNameRegisterScrollView.DrawGUI();

                    GUILayout.Space(2f);
                }

                if (ignoreTargets != null)
                {
                    EditorLayoutTools.ContentTitle("Ignore Targets");

                    using (new ContentsScope())
                    {
                        displayMode = (DisplayMode)EditorGUILayout.EnumPopup(displayMode, GUILayout.Width(80f));

                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                        {
                            foreach (var item in ignoreTargets)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    if (item is Texture texture)
                                    {
                                        EditorGUILayout.LabelField($"{texture.width}x{texture.height}", GUILayout.Width(85f));
                                    }

                                    GUILayout.Space(2f);

                                    switch (displayMode)
                                    {
                                        case DisplayMode.Asset:
                                            EditorGUILayout.ObjectField(item, typeof(Object), false, GUILayout.ExpandWidth(true));
                                            break;

                                        case DisplayMode.Path:
                                            EditorGUILayout.TextField(AssetDatabase.GetAssetPath(item), GUILayout.ExpandWidth(true));
                                            break;
                                    }
                                }
                            }

                            scrollPosition = scrollViewScope.scrollPosition;
                        }
                    }
                }
            }
        }
    }
}