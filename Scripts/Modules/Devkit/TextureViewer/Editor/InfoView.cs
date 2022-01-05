
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace Modules.Devkit.TextureViewer
{
    public abstract class InfoView<TColumn> where TColumn : Enum
    {
        //----- params -----

        //----- field -----

        private Texture warnIcon = null;

        private TColumn[] columns = null;

        //----- property -----

        protected abstract TColumn WarningColumn { get; }

        protected abstract TColumn TextureNameColumn { get; }

        //----- method -----

        public InfoView()
        {
            columns = GetDefaultColumns();
        }

        protected virtual void InitializeStyle()
        {
            if (warnIcon == null)
            {
                var warnIconContent = EditorGUIUtility.IconContent("Warning");

                if (warnIconContent != null)
                {
                    warnIcon = warnIconContent.image;
                }
            }
        }

        public TColumn[] GetDefaultColumns()
        {
            return Enum.GetValues(typeof(TColumn)).Cast<TColumn>().ToArray();
        }

        public void SetCustomColumns(TColumn[] columns)
        {
            this.columns = columns;
        }

        public void DrawRowGUI(Rect rect, int columnIndex, TextureInfo textureInfo)
        {
            InitializeStyle();
            
            var column = columns.ElementAt(columnIndex);

            var value = GetValue(column, textureInfo);

            if (column.Equals(WarningColumn))
            {
                DrawWarningIcon(rect, textureInfo);
            }
            else if (column.Equals(TextureNameColumn))
            {
                var icon = textureInfo.GetTextureIcon();

                DrawNameLabel(rect, icon, value);
            }
            else
            {
                DrawField(rect, value, true);
            }
        }

        private void DrawWarningIcon(Rect rect, TextureInfo textureInfo)
        {
            if (!textureInfo.HasWarning()){ return; }
            
            var warning = textureInfo.GetImportWarning();

            var iconRect = rect;

            iconRect.x += rect.width * 0.5f - warnIcon.width * 0.5f;
            iconRect.y += 2f;
            iconRect.size = new Vector2(warnIcon.width, warnIcon.height);

            GUI.Label(iconRect, new GUIContent(warnIcon, warning));
        }

        private void DrawNameLabel(Rect rect, Texture icon, object value)
        {
            // Icon.

            rect.x += 2f;

            var iconRect = rect;
                            
            iconRect.y += 2f;
            iconRect.size = new Vector2(icon.width, icon.height);

            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon);
            }

            // Label.

            var labelRect = new Rect(rect);
                            
            labelRect.x += iconRect.width + 4f;

            DrawField(labelRect, value, false);
        }

        protected void DrawField(Rect rect, object value, bool aligneCenter)
        {
            if (value is bool b)
            {
                if (aligneCenter)
                {
                    rect.x += rect.width * 0.5f - 8f;
                }

                EditorGUI.Toggle(rect, b);
            }

            if (value is string s)
            {
                if (aligneCenter)
                {
                    var size = EditorStyles.label.CalcSize(new GUIContent(s));

                    rect.x += rect.width * 0.5f - size.x * 0.5f;
                }

                EditorGUI.LabelField(rect, s);
            }
        }

        protected abstract object GetValue(TColumn column, TextureInfo textureInfo);
    }
}