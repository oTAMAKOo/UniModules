
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UniRx;

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

        private Subject<Unit> onRepaintRequest = null;

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

                OnContentsUpdate();
            }
        }

        public Vector2 ScrollPosition { get; set; }

        public abstract Direction Type { get; }

        //----- method -----

        public EditorGUIFastScrollView()
        {
            itemInfos = new ItemInfo[0];
        }

        public void Draw(bool scrollEnable = true, params GUILayoutOption[] options)
        {
            if (scrollEnable)
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

            var layoutMargin = 150f;

            var layoutBegin = ScrollPosition.y - layoutMargin;
            var layoutEnd = ScrollPosition.y + scrollRect.Value.height + layoutMargin;

            startIndex = 0;
            startSpace = 0f;

            //====== 見える範囲までのスペース ======

            for (var i = 0; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.height;

                if (layoutBegin < startSpace + space)
                {
                    startIndex = i;
                    break;
                }

                startSpace += space;
            }

            //====== 見えてる範囲から出るまで検索 ======

            var viewSpace = 0f;

            endIndex = itemInfos.Length;
            endSpace = 0f;

            for (var i = startIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.height;

                if (layoutEnd < startSpace + viewSpace + space)
                {
                    endIndex = i;
                    break;
                }

                viewSpace += space;
            }

            //====== 終端までのスペース ======

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

            var layoutMargin = 150f;

            var layoutBegin = ScrollPosition.x - layoutMargin;
            var layoutEnd = ScrollPosition.x + scrollRect.Value.width + layoutMargin;

            startIndex = 0;
            startSpace = 0f;

            //====== 見える範囲までのスペース ======

            for (var i = 0; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.width;

                if (layoutBegin < startSpace + space)
                {
                    startIndex = i;
                    break;
                }

                startSpace += space;
            }

            //====== 見えてる範囲から出るまで検索 ======

            var viewSpace = 0f;

            endIndex = itemInfos.Length;
            endSpace = 0f;

            for (var i = startIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                var space = itemInfos[i].rect.Value.width;

                if (layoutEnd < startSpace + viewSpace + space)
                {
                    endIndex = i;
                    break;
                }

                viewSpace += space;
            }

            //====== 終端までのスペース ======

            for (var i = endIndex.Value; i < itemInfos.Length; i++)
            {
                if (!itemInfos[i].rect.HasValue) { continue; }

                endSpace += itemInfos[i].rect.Value.width;
            }
        }

        private void DrawScrollContents(params GUILayoutOption[] options)
        {
            var isLayoutEvent = Event.current.type == EventType.Layout;

            // スクロール領域計測用.
            var scrollViewRect = EditorGUILayout.BeginVertical();

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(ScrollPosition, options))
            {
                using (GetScrollDirectionScope())
                {
                    GUILayout.Space(startSpace);

                    for (var i = 0; i < itemInfos.Length; i++)
                    {
                        if (!startIndex.HasValue || i < startIndex.Value) { continue; }

                        if (!endIndex.HasValue || endIndex.Value < i) { continue; }

                        // リストアイテム領域計測用.
                        var rect = EditorGUILayout.BeginVertical();

                        DrawContent(i, itemInfos[i].content);

                        EditorGUILayout.EndVertical();

                        if (isLayoutEvent && rect != Rect.zero)
                        {
                            itemInfos[i].rect = rect;
                        }
                    }

                    GUILayout.Space(endSpace);
                }

                ScrollPosition = scrollViewScope.scrollPosition;
            }

            EditorGUILayout.EndVertical();

            if (isLayoutEvent)
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

            startIndex = null;
            endIndex = null;
            scrollRect = null;
        }

        /// <summary> 再描画要求イベントを発行 </summary>
        public void RequestRepaint()
        {
            if (onRepaintRequest != null)
            {
                onRepaintRequest.OnNext(Unit.Default);
            }
        }

        /// <summary> コンテンツが更新された時のイベント </summary>
        protected virtual void OnContentsUpdate() { }

        /// <summary> 再描画要求イベント </summary>
        public IObservable<Unit> OnRepaintRequestAsObservable()
        {
            return onRepaintRequest ?? (onRepaintRequest = new Subject<Unit>());
        }

        protected abstract void DrawContent(int index, T content);
    }
}
