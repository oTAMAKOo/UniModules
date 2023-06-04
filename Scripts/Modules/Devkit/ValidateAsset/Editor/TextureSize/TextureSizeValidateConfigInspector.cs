
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.ValidateAsset.TextureSize
{
    public enum DisplayMode
    {
        Asset,
        Path,
    }

    [CustomEditor(typeof(TextureSizeValidateConfig))]
    public sealed partial class TextureSizeValidateConfigInspector : Editor
    {
        //----- params -----

        private enum ViewType
        {
            ValidateFolders,
            ManageIgnore,
            Validate,
        }

        //----- field -----

        private TextureSizeValidateConfig instance = null;

        private ValidateTextureSize validateTextureSize = null;

        private List<ValidateData> contents = null;

        private ViewType viewType = ViewType.ValidateFolders;

        private ValidateFoldersView foldersView = null;
        private ManageIgnoreView manageIgnoreView = null;
        private ValidateView validateView = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        private void Initialize()
        {
            if (initialized){ return; }

            viewType = ViewType.ValidateFolders;

            validateTextureSize = new ValidateTextureSize();

            foldersView = new ValidateFoldersView();
            foldersView.Initialize(this);

            manageIgnoreView = new ManageIgnoreView();
            manageIgnoreView.Initialize(this);

            validateView = new ValidateView();
            validateView.Initialize(this);
            
            LoadContents();

            initialized = true;
        }

        void OnDisable()
        {
            SaveContents();
        }

        public override void OnInspectorGUI()
        {
            instance = target as TextureSizeValidateConfig;

            Initialize();

            GUILayout.Space(4f);

            switch (viewType)
            {
                case ViewType.ValidateFolders:
                    foldersView.DrawInspectorGUI();
                    break;

                case ViewType.ManageIgnore:
                    manageIgnoreView.DrawInspectorGUI();
                    break;

                case ViewType.Validate:
                    validateView.DrawInspectorGUI();
                    break;
            }
        }

        private void LoadContents()
        {
            var folderData = Reflection.GetPrivateField<TextureSizeValidateConfig, ValidateData[]>(instance, "validateData");

            if (folderData == null)
            {
                folderData = new ValidateData[0];
            }

            contents = folderData.ToList();

            foldersView.UpdateContents();
        }

        private void SaveContents()
        {
            if (contents == null){ return; }

            var validateData = contents
                .Where(x => !string.IsNullOrEmpty(x.folderGuid))
                .Where(x =>
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(x.folderGuid);
                        return AssetDatabase.IsValidFolder(assetPath);
                    })
                .ToArray();

            Reflection.SetPrivateField(instance, "validateData", validateData);

            UnityEditorUtility.SaveAsset(instance);
        }
    }
}