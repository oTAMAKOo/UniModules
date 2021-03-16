
using UnityEngine;
using UnityEditor;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Inspector;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    [CustomEditor(typeof(TextureAssetTunerConfig))]
    public sealed class TextureAssetTunerConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        private sealed class FolderNameRegisterScrollView : RegisterScrollView<string>
        {
            protected override string CreateNewContent()
            {
                return string.Empty;
            }

            protected override string DrawContent(int index, string content)
            {
                content = EditorGUILayout.DelayedTextField(content);

                return content;
            }
        }

        //----- field -----

        private FolderRegisterScrollView compressFolderView = null;
        private FolderNameRegisterScrollView ignoreCompressFolderNameScrollView = null;

        private FolderRegisterScrollView spriteFolderView = null;
        private FolderNameRegisterScrollView spriteFolderNameScrollView = null;
        private FolderNameRegisterScrollView ignoreSpriteFolderNameScrollView = null;

        private LifetimeDisposable lifetimeDisposable = null;

        private TextureAssetTunerConfig instance = null;

        private bool changeSettingOnImport = false;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as TextureAssetTunerConfig;

            lifetimeDisposable = new LifetimeDisposable();

            //------ Compress Folder ------

            compressFolderView = new FolderRegisterScrollView("Compress Folder", "TextureAssetTunerConfigInspector-CompressFolder");

            compressFolderView.RemoveChildrenFolder = true;

            compressFolderView.OnUpdateContentsAsObservable()
                .Subscribe(x => SaveCompressFolders(x.Select(y => y.asset).ToArray()))
                .AddTo(lifetimeDisposable.Disposable);

            compressFolderView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            compressFolderView.SetContents(instance.CompressFolders);

            //------ Sprite Folder ------

            spriteFolderView = new FolderRegisterScrollView("Sprite Folder", "TextureAssetTunerConfigInspector-SpriteFolder");

            spriteFolderView.RemoveChildrenFolder = true;

            spriteFolderView.OnUpdateContentsAsObservable()
                .Subscribe(x => SaveSpriteFolders(x.Select(y => y.asset).ToArray()))
                .AddTo(lifetimeDisposable.Disposable);

            spriteFolderView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            spriteFolderView.SetContents(instance.SpriteFolders);

            //------ Ignore Compress FolderName ------

            ignoreCompressFolderNameScrollView = new FolderNameRegisterScrollView();

            ignoreCompressFolderNameScrollView.OnUpdateContentsAsObservable()
                .Subscribe(x => SaveIgnoreCompressFolders(x))
                .AddTo(lifetimeDisposable.Disposable);

            ignoreCompressFolderNameScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            ignoreCompressFolderNameScrollView.SetContents(instance.IgnoreCompressFolders);

            //------ Sprite Folder Name ------

            spriteFolderNameScrollView = new FolderNameRegisterScrollView();

            spriteFolderNameScrollView.OnUpdateContentsAsObservable()
                .Subscribe(x => SaveSpriteFolderNames(x))
                .AddTo(lifetimeDisposable.Disposable);

            spriteFolderNameScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            spriteFolderNameScrollView.SetContents(instance.SpriteFolderNames);

            //------ Ignore Sprite FolderName ------

            ignoreSpriteFolderNameScrollView = new FolderNameRegisterScrollView();

            ignoreSpriteFolderNameScrollView.OnUpdateContentsAsObservable()
                .Subscribe(x => SaveIgnoreSpriteFolders(x))
                .AddTo(lifetimeDisposable.Disposable);

            ignoreSpriteFolderNameScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            ignoreSpriteFolderNameScrollView.SetContents(instance.IgnoreSpriteFolders);

            //------ Options ------

            changeSettingOnImport = TextureAssetTunerConfig.Prefs.changeSettingOnImport;
        }

        public override void OnInspectorGUI()
        {
            instance = target as TextureAssetTunerConfig;

            compressFolderView.DrawGUI();

            GUILayout.Space(2f);

            DrawRegisterIgnoreFolderNameGUI(ignoreCompressFolderNameScrollView, "Ignore Compress Folders");

            GUILayout.Space(2f);

            spriteFolderView.DrawGUI();

            GUILayout.Space(2f);

            DrawRegisterIgnoreFolderNameGUI(spriteFolderNameScrollView, "Sprite FolderName");

            GUILayout.Space(2f);

            DrawRegisterIgnoreFolderNameGUI(ignoreSpriteFolderNameScrollView, "Ignore Sprite Folders");

            GUILayout.Space(2f);

            EditorLayoutTools.Title("Options");

            using (new ContentsScope())
            {
                EditorGUI.BeginChangeCheck();

                changeSettingOnImport = EditorGUILayout.Toggle("Change setting on import", changeSettingOnImport);

                if (EditorGUI.EndChangeCheck())
                {
                    TextureAssetTunerConfig.Prefs.changeSettingOnImport = changeSettingOnImport;
                }

                GUILayout.Space(3f);

                EditorGUILayout.HelpBox("When this flag is enabled, the settings will be changed at the time of import..", MessageType.Info);
            }
        }

        private void DrawRegisterIgnoreFolderNameGUI(FolderNameRegisterScrollView scrollView, string title)
        {
            var scrollViewHeight = Mathf.Min(scrollView.Contents.Length * 18f, 150f);

            if (EditorLayoutTools.Header(title, string.Format("TextureAssetTunerConfigInspector-{0}", title)))
            {
                using (new ContentsScope())
                {
                    scrollView.DrawGUI(GUILayout.Height(scrollViewHeight));
                }
            }
        }

        private void SaveCompressFolders(Object[] folders)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "compressFolders", folders);
        }

        private void SaveIgnoreCompressFolders(string[] folders)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "ignoreCompressFolders", folders);
        }

        private void SaveSpriteFolders(Object[] folders)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "spriteFolders", folders);
        }

        private void SaveSpriteFolderNames(string[] folderNames)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "spriteFolderNames", folderNames);
        }

        private void SaveIgnoreSpriteFolders(string[] folders)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "ignoreSpriteFolders", folders);
        }
    }
}
