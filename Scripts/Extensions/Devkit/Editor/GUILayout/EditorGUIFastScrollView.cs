
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Extensions.Devkit
{
    public abstract class EditorGUIFastScrollView<T>
    {
        //----- params -----

        public enum Direction
        {
            Vertical,
            Horizontal,
        }

        private class ItemInfo
        {
            public T content { get; private set; }
            public Rect? rect { get; set; }

            public ItemInfo(T content)
            {
                this.content = content;
                rect = null;
            }
        }

        //----- field -----

        private ItemInfo[] itemInfos = null;
        private Rect? scrollRect = null;

        private int? startIndex = null;
        private int? endIndex = null;

        private float startSpace = 0f;
        private float endSpace = 0f;

        //----- property -----

        public T[] Contents
        {
            get { return itemInfos.Select(x => x.content).ToArray(); }

            set
            {
                Refresh();

                itemInfos = value == null ?
                    new ItemInfo[0] :
                    value.Select(x => new ItemInfo(x)).ToArray();
            }
        }

        public Vector2 ScrollPosition { get; set; }

        public abstract Direction Type { get; }

        //----- method -----

        public void Draw(bool scrollEnable = true, params GUILayoutOption[] options)
        {
            if(scrollEnable)
            {
                switch (Type)
                {
                    case Direction.Horizontal:
                        LayoutHorizontal();
                        break;

                    case Direction.Vertical:
                        LayoutVertical();
                        break;
                }

                DrawScrollContents(options);
            }
            else
            {
                DrawContents(options);
            }
        }

        private void LayoutVertical()
        {
            if (Event.current.type != EventType.Layout) { return; }

            if (!scrollRect.HasValue) { return; }

            var layoutMargin = scrollRect.Value.height;

            startIndex = 0;
            startSpace = 0f;

            // 見える範囲までのスペース.
            for (var i = 0; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.height;

                if (ScrollPosition.y - layoutMargin < startSpace + space)
                {
                    startIndex = i;
                    break;
                }

                startSpace += space;
            }

            endIndex = itemInfos.Length;
            endSpace = 0f;

            var viewSpace = 0f;

            // 見えてる範囲から出るまで検索.
            for (var i = startIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.height;

                if (ScrollPosition.y + scrollRect.Value.height + layoutMargin < startSpace + viewSpace + space)
                {
                    endIndex = i;
                    break;
                }

                viewSpace += space;
            }

            // 終端までのスペース.
            for (var i = endIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                endSpace += itemInfos[i].rect.Value.height;
            }
        }

        private void LayoutHorizontal()
        {
            if (Event.current.type != EventType.Layout) { return; }

            if (!scrollRect.HasValue) { return; }

            var layoutMargin = scrollRect.Value.width;

            startIndex = 0;
            startSpace = 0f;

            // 見える範囲までのスペース.
            for (var i = 0; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.width;

                if (ScrollPosition.x - layoutMargin < startSpace + space)
                {
                    startIndex = i;
                    break;
                }

                startSpace += space;
            }

            endIndex = itemInfos.Length;
            endSpace = 0f;

            var viewSpace = 0f;

            // 見えてる範囲から出るまで検索.
            for (var i = startIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.width;

                if (ScrollPosition.x + scrollRect.Value.width + layoutMargin < startSpace + viewSpace + space)
                {
                    endIndex = i;
                    break;
                }

                viewSpace += space;
            }

            // 終端までのスペース.
            for (var i = endIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                endSpace += itemInfos[i].rect.Value.width;
            }
        }

        private void DrawScrollContents(params GUILayoutOption[] options)
        {
            // スクロール領域計測用.
            var scrollViewRect = EditorGUILayout.BeginVertical();

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(ScrollPosition, options))
            {
                using (GetScrollDirectionScope())
                {
                    GUILayout.Space(startSpace);

                    for (var i = 0; i < itemInfos.Length; i++)
                    {
                        if (startIndex.HasValue && i < startIndex.Value) { continue; }

                        if (endIndex.HasValue && endIndex.Value < i) { continue; }

                        // リストアイテム領域計測用.
                        var rect = EditorGUILayout.BeginVertical();

                        DrawContent(i, itemInfos[i].content);

                        EditorGUILayout.EndVertical();

                        if (Event.current.type == EventType.Repaint)
                        {
                            if (!itemInfos[i].rect.HasValue)
                            {
                                itemInfos[i].rect = rect;
                            }
                        }
                    }

                    GUILayout.Space(endSpace);
                }

                ScrollPosition = scrollViewScope.scrollPosition;
            }

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                scrollRect = scrollViewRect;
            }
        }

        private void DrawContents(params GUILayoutOption[] options)
        {
            using (GetScrollDirectionScope(options))
            {
                for (var i = 0; i < itemInfos.Length; i++)
                {
                    DrawContent(i, itemInfos[i].content);
                }
            }
        }

        private GUI.Scope GetScrollDirectionScope(params GUILayoutOption[] options)
        {
            GUI.Scope result = null;

            switch (Type)
            {
                case Direction.Horizontal:
                    result = new EditorGUILayout.HorizontalScope(options);
                    break;
                case Direction.Vertical:
                    result = new EditorGUILayout.VerticalScope(options);
                    break;
            }

            return result;
        }

        public void Refresh()
        {
            if (itemInfos != null)
            {
                for (var i = 0; i < itemInfos.Length; i++)
                {
                    itemInfos[i].rect = null;
                }
            }

            scrollRect = null;
        }

        protected abstract void DrawContent(int index, T content);
    }
}
