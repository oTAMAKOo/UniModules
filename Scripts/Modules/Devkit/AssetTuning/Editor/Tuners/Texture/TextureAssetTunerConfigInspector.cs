
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.AssetBundles;
using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    [CustomEditor(typeof(TextureAssetTunerConfig))]
    public sealed class TextureAssetTunerConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private CompressFolderView compressFolderView = null;

        private TextureAssetTunerConfig instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as TextureAssetTunerConfig;

            compressFolderView = new CompressFolderView();
            compressFolderView.Initialize(instance, this);
        }

        public override void OnInspectorGUI()
        {
            instance = target as TextureAssetTunerConfig;

            compressFolderView.DrawGUI();
        }
    }

    public class CompressFolderView : LifetimeDisposable
    {
        //----- params -----

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        private class CompressFolderInfo
        {
            public string assetPath = null;
            public Object asset = null;
        }

        private class CompressFolderScrollView : EditorGUIFastScrollView<CompressFolderInfo>
        {
            private Subject<CompressFolderInfo[]> onUpdateContents = null;

            public AssetViewMode AssetViewMode { get; set; }

            public override Direction Type { get { return Direction.Vertical; } }

            protected override void DrawContent(int index, CompressFolderInfo content)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    switch (AssetViewMode)
                    {
                        case AssetViewMode.Asset:
                            {
                                EditorGUI.BeginChangeCheck();

                                var folder = EditorGUILayout.ObjectField(content.asset, typeof(Object), false);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (CheckFolderAsset(folder))
                                    {
                                        if (CheckParentFolderRegisted(folder, Contents))
                                        {
                                            var newContent = new CompressFolderInfo()
                                            {
                                                asset = folder,
                                                assetPath = AssetDatabase.GetAssetPath(folder),
                                            };

                                            // 一旦ローカル配列に変換してから上書き.
                                            var contents = Contents.ToArray();

                                            contents[index] = newContent;

                                            // 親が登録された場合子階層を除外.
                                            var removeChildrenInfos = GetRemoveChildrenFolders(folder, Contents);

                                            if (removeChildrenInfos.Any())
                                            {
                                                EditorUtility.DisplayDialog("Updated", "Deleted registration of child folders.", "Close");

                                                contents = contents.Where(x => !removeChildrenInfos.Contains(x)).ToArray();
                                            }

                                            Contents = contents;
                                        }                                        

                                        UpdateContens();
                                    }
                                }
                            }
                            break;

                        case AssetViewMode.Path:
                            GUILayout.Label(content.assetPath, EditorLayoutTools.TextAreaStyle);
                            break;
                    }

                    var toolbarPlusTexture = EditorGUIUtility.FindTexture("Toolbar Minus");

                    if (GUILayout.Button(new GUIContent(toolbarPlusTexture), EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                    {
                        var list = Contents.ToList();

                        list.RemoveAt(index);

                        Contents = list.ToArray();

                        UpdateContens();
                    }
                }
            }

            private void UpdateContens()
            {
                if (onUpdateContents != null)
                {
                    onUpdateContents.OnNext(Contents);
                }

                RequestRepaint();
            }

            public IObservable<CompressFolderInfo[]> OnUpdateContentsAsObservable()
            {
                return onUpdateContents ?? (onUpdateContents = new Subject<CompressFolderInfo[]>());
            }
        }

        //----- field -----

        private AssetViewMode assetViewMode = AssetViewMode.Asset;
        private List<CompressFolderInfo> compressFolderInfos = null;
        private CompressFolderScrollView compressFolderScrollView = null;
        private Texture toolbarPlusTexture = null;

        private TextureAssetTunerConfig instance = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public void Initialize(TextureAssetTunerConfig instance, TextureAssetTunerConfigInspector inspector)
        {
            if (initialized) { return; }

            this.instance = instance;

            compressFolderScrollView = new CompressFolderScrollView();
            compressFolderScrollView.AssetViewMode = assetViewMode;

            compressFolderInfos = instance.CompressFolders
                .Select(x =>
                    {
                        var info = new CompressFolderInfo()
                        {
                            asset = x,
                            assetPath = x != null ? AssetDatabase.GetAssetPath(x) : string.Empty,
                        };

                        return info;
                    })
                .ToList();

            compressFolderScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ => inspector.Repaint())
                .AddTo(Disposable);

            compressFolderScrollView.OnUpdateContentsAsObservable()
                .Subscribe(x =>
                   {
                       compressFolderInfos = x.ToList();
                       SaveTargetFolders(x);
                   })
                .AddTo(Disposable);

            compressFolderScrollView.Contents = compressFolderInfos.ToArray();

            toolbarPlusTexture = EditorGUIUtility.FindTexture("Toolbar Plus");

            initialized = true;
        }

        public void DrawGUI()
        {
            if (EditorLayoutTools.DrawHeader("Compress Folder", "TextureAssetTunerConfigInspector-CompressFolder"))
            {
                using (new ContentsScope())
                {
                    GUILayout.Space(2f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();

                        assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

                        if (EditorGUI.EndChangeCheck())
                        {
                            compressFolderScrollView.AssetViewMode = assetViewMode;
                        }

                        GUILayout.FlexibleSpace();
                        
                        if (GUILayout.Button(new GUIContent(toolbarPlusTexture), EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                        {
                            var compressFolder = new CompressFolderInfo()
                            {
                                asset = null,
                                assetPath = string.Empty,
                            };

                            compressFolderInfos.Add(compressFolder);

                            compressFolderScrollView.Contents = compressFolderInfos.ToArray();

                            SaveTargetFolders(compressFolderInfos);
                        }
                    }

                    GUILayout.Space(4f);

                    var scrollViewHeight = Mathf.Min(compressFolderInfos.Count * 18f, 150f);

                    compressFolderScrollView.Draw(true, GUILayout.Height(scrollViewHeight));
                }
            }
        }

        private static bool CheckParentFolderRegisted(Object folder, CompressFolderInfo[] folderInfos)
        {
            var assetPath = AssetDatabase.GetAssetPath(folder);

            var registedFolderInfo = folderInfos
                .Where(x => x.asset != null && !string.IsNullOrEmpty(x.assetPath))
                .FirstOrDefault(x => x.assetPath.Length <= assetPath.Length && assetPath.StartsWith(x.assetPath));

            if (registedFolderInfo != null)
            {
                EditorUtility.DisplayDialog("Register failed", "This folder is registed.", "Close");
                
                EditorGUIUtility.PingObject(registedFolderInfo.asset);

                return false;
            }

            return true;
        }

        // 親フォルダが登録された際に削除されるフォルダ情報取得.
        private static CompressFolderInfo[] GetRemoveChildrenFolders(Object folder, CompressFolderInfo[] folderInfos)
        {
            if (folder == null) { return new CompressFolderInfo[0];}

            var assetPath = AssetDatabase.GetAssetPath(folder);

            return folderInfos.Where(x => assetPath.Length < x.assetPath.Length && x.assetPath.StartsWith(assetPath)).ToArray();
        }

        private static bool CheckFolderAsset(Object folder)
        {
            if (folder == null) { return true; }

            var assetPath = AssetDatabase.GetAssetPath(folder);

            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                EditorUtility.DisplayDialog("Register failed", "This asset not a folder.", "Close");

                return false;
            }

            return true;
        }

        private void SaveTargetFolders(IEnumerable<CompressFolderInfo> folderInfos)
        {
            var folders = folderInfos.Select(x => x.asset).ToArray();

            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "compressFolders", folders);
        }
    }
}
