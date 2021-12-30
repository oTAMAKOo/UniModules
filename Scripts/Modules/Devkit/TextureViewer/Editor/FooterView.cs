
using UnityEngine;
using UnityEditor;
using System;

namespace Modules.Devkit.TextureViewer
{
    public sealed class FooterView
    {
        //----- params -----

        //----- field -----

        private TextureInfo selectionTextureInfo = null;

        private GUIStyle assetPathLabelStyle = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            if (initialized) { return; }

            initialized = true;
        }

        private void InitializeStyle()
        {
            if (assetPathLabelStyle == null)
            {
                assetPathLabelStyle = new GUIStyle("ToolbarLabel");

                assetPathLabelStyle.alignment = TextAnchor.MiddleLeft;
                assetPathLabelStyle.fontSize = 11;
            }
        }

        public void DrawGUI()
        {
            InitializeStyle();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                GUILayout.Space(20f);

                var assetPath = selectionTextureInfo != null ? selectionTextureInfo.AssetPath : string.Empty;

                EditorGUILayout.SelectableLabel(assetPath, assetPathLabelStyle);
            }
        }

        public void SetSelection(TextureInfo textureInfo)
        {
            selectionTextureInfo = textureInfo;
        }
    }
}