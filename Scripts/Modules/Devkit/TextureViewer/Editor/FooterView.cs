
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

        private GUIStyle loadingProgressLabelStyle = null;
        
        private string loadingProgressText = null;

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

            if (loadingProgressLabelStyle == null)
            {
                loadingProgressLabelStyle = new GUIStyle(EditorStyles.label);

                loadingProgressLabelStyle.alignment = TextAnchor.MiddleRight;
                loadingProgressLabelStyle.fontSize = 10;
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

                GUILayout.FlexibleSpace();

                if (!string.IsNullOrEmpty(loadingProgressText))
                {
                    var size = EditorStyles.label.CalcSize(new GUIContent(loadingProgressText));

                    EditorGUILayout.LabelField(loadingProgressText, loadingProgressLabelStyle, GUILayout.Width(size.x));

                    GUILayout.Space(5f);
                }
            }
        }
        public void SetSelection(TextureInfo textureInfo)
        {
            selectionTextureInfo = textureInfo;
        }

        public void SetLoadingProgressText(string text)
        {
            loadingProgressText = text;
        }
    }
}