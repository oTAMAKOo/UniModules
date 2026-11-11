
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Extensions.Devkit
{
    public static class EditorGUISelectableHelpBox
    {
        //----- params -----

        private const float IconSize = 32f;

        private const float IconTextSpace = 4f;

        //----- field -----

        private static GUIStyle selectableLabelStyle = null;

        private static Dictionary<MessageType, Texture> iconCache = null;

        //----- property -----

        //----- method -----

        /// <summary> テキスト選択可能なHelpBox描画 </summary>
        public static void Draw(string message, MessageType messageType)
        {
            var helpBoxStyle = EditorStyles.helpBox;
            var icon = GetIconTexture(messageType);
            var textStyle = GetSelectableLabelStyle();

            var padding = helpBoxStyle.padding;
            var hasIcon = icon != null;

            var textOffsetX = hasIcon ? IconSize + IconTextSpace : 0f;
            var viewWidth = EditorGUIUtility.currentViewWidth;
            var textWidth = viewWidth - textOffsetX - padding.horizontal - 8f;

            var textContent = new GUIContent(message);
            var textHeight = textStyle.CalcHeight(textContent, textWidth);
            var totalHeight = Mathf.Max(textHeight + padding.vertical, hasIcon ? IconSize + padding.vertical : 0f);

            var rect = EditorGUILayout.GetControlRect(false, totalHeight);

            // 背景描画.
            GUI.Box(rect, GUIContent.none, helpBoxStyle);

            // アイコン描画.
            if (hasIcon)
            {
                var iconRect = new Rect(
                    rect.x + padding.left,
                    rect.y + (rect.height - IconSize) * 0.5f,
                    IconSize, IconSize);

                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            // テキスト描画(選択可能).
            var textRect = new Rect(
                rect.x + padding.left + textOffsetX,
                rect.y + padding.top,
                rect.width - padding.horizontal - textOffsetX,
                rect.height - padding.vertical);

            EditorGUI.SelectableLabel(textRect, message, textStyle);
        }

        private static GUIStyle GetSelectableLabelStyle()
        {
            if (selectableLabelStyle == null)
            {
                selectableLabelStyle = new GUIStyle(EditorStyles.label);
                selectableLabelStyle.fontSize = EditorStyles.helpBox.fontSize;
                selectableLabelStyle.wordWrap = true;
                selectableLabelStyle.richText = false;
            }

            return selectableLabelStyle;
        }

        private static Texture GetIconTexture(MessageType messageType)
        {
            if (iconCache == null)
            {
                iconCache = new Dictionary<MessageType, Texture>();
            }

            if (messageType == MessageType.None) { return null; }

            if (iconCache.TryGetValue(messageType, out var cached))
            {
                return cached;
            }

            var iconName = string.Empty;

            switch (messageType)
            {
                case MessageType.Error:
                    iconName = "console.erroricon";
                    break;

                case MessageType.Warning:
                    iconName = "console.warnicon";
                    break;

                case MessageType.Info:
                    iconName = "console.infoicon";
                    break;
            }

            var content = EditorGUIUtility.IconContent(iconName);

            if (content == null || content.image == null) { return null; }

            iconCache[messageType] = content.image;

            return content.image;
        }
    }
}
