
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.TextureAssetCompress
{
    [CustomEditor(typeof(TextureAssetCompressConfigs))]
    public class TextureAssetCompressConfigsInspector : UnityEditor.Editor
    {
        //----- params -----

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;
        private AssetViewMode assetViewMode = AssetViewMode.Asset;

        private TextureAssetCompressConfigs instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as TextureAssetCompressConfigs;

            var e = Event.current;

            var removeTargets = new List<int>();

            EditorLayoutTools.DrawLabelWithBackground("Compress Folder", new Color(0.2f, 0.8f, 0.14f, 0.8f));

            using (new ContentsScope())
            {
                var change = false;

                var compressFolders = instance.CompressFolders.ToList();

                GUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(50f)))
                    {
                        compressFolders.Add(null);
                        change = true;
                    }
                }

                if (compressFolders.Any())
                {
                    GUILayout.Space(4f);

                    var targetFolderPaths = compressFolders
                        .Select(x => AssetDatabase.GetAssetPath(x))
                        .ToArray();

                    var contentHeight = 20f;

                    var count = compressFolders.Count;
                    var scrollViewHeight = Mathf.Min(contentHeight * count + 5f, 300f);

                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(scrollViewHeight)))
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var compressFolder = compressFolders[i];

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUI.BeginChangeCheck();

                                    switch (assetViewMode)
                                    {
                                        case AssetViewMode.Asset:
                                            compressFolder = EditorGUILayout.ObjectField(compressFolder, typeof(Object), false);
                                            break;

                                        case AssetViewMode.Path:
                                            GUILayout.Label(targetFolderPaths[i], EditorLayoutTools.TextAreaStyle);
                                            break;
                                    }

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        if (compressFolder != null)
                                        {
                                            if (IsFolderAsset(compressFolder))
                                            {
                                                if (CheckParentFolderRegisted(compressFolder, targetFolderPaths))
                                                {
                                                    compressFolders[i] = compressFolder;
                                                    change = true;

                                                    // 親が登録された場合子階層を除外.
                                                    var removeChildrenFolders = GetRemoveChildrenFolders(compressFolder, targetFolderPaths);

                                                    if (removeChildrenFolders.Any())
                                                    {
                                                        EditorUtility.DisplayDialog("Updated", "Deleted registration of child folders.", "Close");

                                                        var removeTargetAssets = removeChildrenFolders.Select(x => AssetDatabase.LoadMainAssetAtPath(x));

                                                        compressFolders = compressFolders.Where(x => !removeTargetAssets.Contains(x)).ToList();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20f)))
                                {
                                    removeTargets.Add(i);
                                    change = true;
                                }
                            }

                            GUILayout.Space(2f);
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }
                }

                if (removeTargets.Any())
                {
                    foreach (var removeTarget in removeTargets)
                    {
                        compressFolders.RemoveAt(removeTarget);
                    }
                }

                if (change)
                {
                    SaveTargetFolders(compressFolders.ToArray());
                }
            }
        }

        private bool IsFolderAsset(Object folder)
        {
            var assetPath = AssetDatabase.GetAssetPath(folder);

            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                EditorUtility.DisplayDialog("Register failed", "This asse not a folder.", "Close");

                return false;
            }

            return true;
        }

        private bool CheckParentFolderRegisted(Object folder, string[] targetFolderPaths)
        {
            var assetPath = AssetDatabase.GetAssetPath(folder);

            var registedParentAssetPath = targetFolderPaths.FirstOrDefault(x => x.Length < assetPath.Length && assetPath.StartsWith(x));

            if (!string.IsNullOrEmpty(registedParentAssetPath))
            {
                EditorUtility.DisplayDialog("Register failed", "This folder parent is registed.", "Close");

                var asset = AssetDatabase.LoadMainAssetAtPath(registedParentAssetPath);
                EditorGUIUtility.PingObject(asset);

                return false;
            }

            return true;
        }

        /// <summary> 親フォルダが登録された際に削除されるフォルダパス取得. </summary>
        private string[] GetRemoveChildrenFolders(Object folder, string[] targetFolderPaths)
        {
            var assetPath = AssetDatabase.GetAssetPath(folder);

            return targetFolderPaths.Where(x => assetPath.Length < x.Length && x.StartsWith(assetPath)).ToArray();
        }

        private void SaveTargetFolders(Object[] compressFolders)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetCompressConfigsInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "compressFolders", compressFolders);
        }
    }
}
