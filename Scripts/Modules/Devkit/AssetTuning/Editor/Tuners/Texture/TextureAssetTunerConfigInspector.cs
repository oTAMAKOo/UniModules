
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
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

        //----- field -----

        private FolderRegisterScrollView compressFolderView = null;
        private FolderRegisterScrollView spriteFolderView = null;

        private LifetimeDisposable lifetimeDisposable = null;

        private TextureAssetTunerConfig instance = null;

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
                .Subscribe(x => SaveCompressFolders(x))
                .AddTo(lifetimeDisposable.Disposable);

            compressFolderView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            compressFolderView.SetContents(instance.CompressFolders);

            //------ Sprite Folder ------

            spriteFolderView = new FolderRegisterScrollView("Sprite Folder", "TextureAssetTunerConfigInspector-SpriteFolder");

            spriteFolderView.RemoveChildrenFolder = true;

            spriteFolderView.OnUpdateContentsAsObservable()
                .Subscribe(x => SaveSpriteFolders(x))
                .AddTo(lifetimeDisposable.Disposable);

            spriteFolderView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            spriteFolderView.SetContents(instance.SpriteFolders);
        }

        public override void OnInspectorGUI()
        {
            instance = target as TextureAssetTunerConfig;

            compressFolderView.DrawGUI();

            GUILayout.Space(2f);

            spriteFolderView.DrawGUI();
        }

        private void SaveCompressFolders(Object[] folders)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "compressFolders", folders);
        }

        private void SaveSpriteFolders(Object[] folders)
        {
            UnityEditorUtility.RegisterUndo("TextureAssetTunerConfigInspector-Undo", instance);

            Reflection.SetPrivateField(instance, "spriteFolders", folders);
        }
    }
}
